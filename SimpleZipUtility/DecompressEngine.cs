using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using SimpleZipUtility.Interfaces;
using SimpleZipUtility.UtilityExtensions;

namespace SimpleZipUtility
{
    public class DecompressEngine : BaseEngine, IArchivator
    {
        private static byte[] GZIP_HEADER_BYTES = new byte[] { 0x1f, 0x8b, 8, 0, 0, 0, 0, 0, 4, 0 };

        public DecompressEngine(IGzipAction gzip, int queueCapacity)
            : base(gzip, queueCapacity)
        {

        }

        public override void WriteStreamSegmentsToQueue(Stream init)
        {
            if (init.Length == 0)
                throw new ArgumentException("empty compressed file");

            int i = 1;

            bool result = false;

            if (init.Length < _bufferSize)
                _bufferSize = init.Length;

            byte[] buffer = new byte[_bufferSize];

            long readBytes = 0;
            long totalBytes = 0;

            List<byte> newbuffer = new List<byte>();

            while ((readBytes = init.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (_concurentQueue.Size > _capacity) //Queue limit
                {
                    _stopReadEvent.Reset();
                    _stopReadEvent.WaitOne();
                }

                if (readBytes < buffer.Length)
                    buffer = buffer.TruncateBuffer(readBytes);
                
                newbuffer.AddRange(buffer);
                
                ProcessBuffer(newbuffer, ref i);

                totalBytes += readBytes;

                if (totalBytes == init.Length)
                {
                    _concurentQueue.Enqueue(new Element { Number = i++, Data = newbuffer.ToArray() });
                }
            }

            _readComplete = true;
        }

        private void ProcessBuffer(List<byte> buffer, ref int i)
        {
            byte[] tail = new byte[0];

            while (buffer.Count > 0)
            {
                byte[] segment = null;
                bool found = FindSegmentStartAndLastIndex(buffer.ToArray(), ref segment);

                if (found)
                {
                    tail = new byte[buffer.Count - segment.Length];
                    Array.Copy(buffer.ToArray(), segment.Length, tail, 0, tail.Length);
                    buffer.Clear();
                    buffer.AddRange(tail);
                    _concurentQueue.Enqueue(new Element{Number = i++, Data = segment});
                }
                else
                {
                    break;
                }
            }
        }

        public bool FindSegmentStartAndLastIndex(byte[] buffer, ref byte[] segment)
        {
            List<long> indexList = new List<long>();

            bool isGzip = false;
            for (long i = 0; i < buffer.Length; i++)
            {
                if (buffer[i] == GZIP_HEADER_BYTES[0])
                {
                    var tail = buffer.Length - i;
                    if (tail < 10)
                        break;

                    isGzip = true;
                    for (int j = 0; j < 9; j++)
                    {
                        if (GZIP_HEADER_BYTES[j] != buffer[i + j])
                            isGzip &= false;
                    }

                    if (isGzip)
                    {
                        indexList.Add(i);
                    }
                }
            }

            var indexes = indexList.ToArray();
            if (indexes.Length < 2)
            {
                segment = buffer;
                isGzip = false;
            }
            else
            {
                segment = new byte[indexes[1] - indexes[0]];
                Array.Copy(buffer, indexes[0], segment, 0, segment.Length);
                isGzip = true;
            }

            return isGzip;
        }
    }
}
