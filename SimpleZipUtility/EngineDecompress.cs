using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using SimpleZipUtility.Interfaces;
using SimpleZipUtility.UtilityExtensions;

namespace SimpleZipUtility
{
    public class EngineDecompress : BaseEngine, IArchivator
    {
        private static byte[] GZIP_HEADER_BYTES = new byte[] { 0x1f, 0x8b, 8, 0, 0, 0, 0, 0, 4, 0 };

        public EngineDecompress(IGzipAction gzip)
            : base(gzip)
        {

        }

        public bool DoAction(Stream input, Stream output, long bufferSize)
        {
            if (input.Length == 0)
                throw new ArgumentException("empty file");

            _bufferSize = bufferSize;

            var t1 = new Thread(() => WriteStreamSegmentsToSharedQueue(input)) { IsBackground = true };
            var t2 = new Thread(() => DequeueSegmentToStream(output, input.Length));

            t1.Start();
            t2.Start();

            _completeEvent.WaitOne();
            return true;
        }

        public override void WriteStreamSegmentsToSharedQueue(Stream init)
        {
            if (init.Length == 0)
                throw new ArgumentException("empty compressed file");

            bool result = false;

            if (init.Length < _bufferSize)
                _bufferSize = init.Length;

            byte[] buffer = new byte[_bufferSize];

            long readBytes = 0;
            long totalBytes = 0;

            List<byte> newbuffer = new List<byte>();

            while ((readBytes = init.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (readBytes < buffer.Length)
                    buffer = buffer.TruncateBuffer(readBytes);
                
                newbuffer.AddRange(buffer);

                ProcessBuffer(newbuffer);

                totalBytes += readBytes;

                if (totalBytes == init.Length)
                {
                    Enqueue(newbuffer.ToArray());
                }
                _mainEvent.Set();
            }

            _readComplete = true;
            _mainEvent.Set();
        }

        internal void Enqueue(byte[] data)
        {
            try
            {
                _rw.EnterWriteLock();
                _sharedQueue.Enqueue(data);
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        internal void ProcessBuffer(List<byte> buffer)
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

                    Enqueue(segment);
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
