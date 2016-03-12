using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using SimpleZipUtility.Interfaces;
using SimpleZipUtility.UtilityExtensions;
using SimpleZipUtility.Queues;

namespace SimpleZipUtility
{
    public class CompressEngine: BaseEngine, IArchivator
    {
        volatile int _countDown = 0;
        readonly int _processors = Environment.ProcessorCount;

        public CompressEngine(IGzipAction gzip, int queueCapacity)
            : base(gzip, queueCapacity)
        {
            
        }

        public bool DoAction(Stream init, Stream archive, long bufferSize)
        {
            if (init.Length == 0)
                throw new ArgumentException("empty file");

            _bufferSize = bufferSize;
            
            var t1 = new Thread(() => WriteStreamSegmentsToQueue(init));
            t1.Start();

            for (int i = 0; i < _processors; i++)
            {
                new Thread(() => ProcessSegment(_concurentMinPQ)).Start();
            }

            var t2 = new Thread(() => WriteProcessedSegmentsToStream(archive));
            t2.Start();

            _completeEvent.WaitOne();
            
            return true;
        }

        public override void WriteStreamSegmentsToQueue(Stream init)
        {
            int i = 1;
            long bytesRead = 0;
            byte[] buffer = new byte[_bufferSize];
            while ((bytesRead = init.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (bytesRead < buffer.Length)
                    buffer = buffer.TruncateBuffer(bytesRead);

                byte[] copy = new byte[buffer.Length];
                Array.Copy(buffer, copy, buffer.Length);
                _concurentQueue.Enqueue(new Element { Number = i++, Data = copy });

                _totalBytesRead += bytesRead;
            }
            _readComplete = true;
        }

        public override void WriteProcessedSegmentsToStream(Stream toHdd)
        {
            while (_countDown < _processors || !_concurentMinPQ.IsEmpty())
            {
                if (_countDown == _processors)
                {
                    Element aux = _concurentMinPQ.Dequeue();

                    if (aux != null)
                    {
                        toHdd.Write(aux.Data, 0, aux.Data.Length);
                        aux = null;
                    }
                }
            }

            _completeEvent.Set();
        }

        public void ProcessSegment(Concurrent<Element> q)
        {
            while (!_readComplete || !_concurentQueue.IsEmpty())
            {
                Element aux = _concurentQueue.Dequeue();

                if (aux != null)
                {
                    byte[] processedData = _gzip.DoWork(aux.Data);
                    q.Enqueue(new Element { Number = aux.Number, Data = processedData});
                    aux = null;
                }
            }

            Interlocked.Increment(ref _countDown);
        }
    }
}
