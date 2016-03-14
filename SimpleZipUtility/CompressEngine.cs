using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using SimpleZipUtility.Interfaces;
using SimpleZipUtility.UtilityExtensions;
using SimpleZipUtility.Queues;
using System.Diagnostics;

namespace SimpleZipUtility
{
    public class CompressEngine: BaseEngine, IArchivator
    {
        volatile int _countDown = 0;
        readonly int _processors = Environment.ProcessorCount;

        private int _capacity;

        public CompressEngine(IGzipAction gzip, int queueCapacity)
            : base(gzip, queueCapacity)
        {
            _capacity = queueCapacity;
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
                if (_concurentQueue.Size > _capacity) //Queue limit
                {
                    _stopEvent.Reset();
                    _stopEvent.WaitOne();
                }
                if (bytesRead < buffer.Length)
                    buffer = buffer.TruncateBuffer(bytesRead);

                byte[] copy = new byte[buffer.Length];
                Array.Copy(buffer, copy, buffer.Length);
                _concurentQueue.Enqueue(new Element { Number = i++, Data = copy });
                copy = null;

                _totalBytesRead += bytesRead;

#if DEBUG
                //Debug.WriteLine(string.Format("QueueSize: {0}, MinPQSize: {1}, TotalKBytes Read: {2}", _concurentQueue.Size, _concurentMinPQ.Size, _totalBytesRead/1024));
#endif
            }
            _readComplete = true;
        }

        public override void WriteProcessedSegmentsToStream(Stream toHdd)
        {
            int prevNumber = 0;
            while (_countDown < _processors || !_concurentMinPQ.IsEmpty)
            {
                Element min = _concurentMinPQ.Peak();

                if (min != null && min.Number - prevNumber == 1)
                {
#if DEBUG
                    Debug.WriteLine(string.Format("N:{0}",min.Number));
#endif
                    min = _concurentMinPQ.Dequeue();
                    toHdd.Write(min.Data, 0, min.Data.Length);
                    prevNumber = min.Number;
                    min = null;

                    if (_concurentQueue.Size == 0 && _concurentMinPQ.IsEmpty) //Queue limit
                        _stopEvent.Set();
                }
            }

            _completeEvent.Set();
        }

        public void ProcessSegment(Concurrent<Element> minPQ)
        {
            while (!_readComplete || !_concurentQueue.IsEmpty)
            {
                if (minPQ.Size > _capacity) //MinPQ limit
                    continue;

                Element aux = _concurentQueue.Dequeue();

                if (aux != null)
                {
                    byte[] processedData = _gzip.DoWork(aux.Data);
                    minPQ.Enqueue(new Element { Number = aux.Number, Data = processedData});
                    aux = null;
                }
             }

            Interlocked.Increment(ref _countDown);
        }
    }
}
