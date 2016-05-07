using SimpleZipUtility.Interfaces;
using SimpleZipUtility.Queues;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace SimpleZipUtility
{
    public abstract class BaseEngine
    {
        public long TotalBytesRead
        {
            get { return _totalBytesRead; }
        }

        public readonly int _processors;

        internal ManualResetEvent _completeEvent = new ManualResetEvent(false);
        internal ManualResetEvent _stopReadEvent = new ManualResetEvent(false);
        
        internal long _totalBytesRead;
        internal long _bufferSize;

        internal volatile bool _readComplete = false;
        internal IGzipAction _gzip;

        internal IQueable<Element> _concurentQueue;
        internal IQueable<Element> _concurentMinPQ;

        internal int _capacity;
        internal int _processDone;

        internal IQueable<Element> _q;
        private IQueable<Element> _minPQ;
        
        public BaseEngine(IGzipAction gzip, int queueCapacity)
        {
            _gzip = gzip;
            _q = new SimpleQueue(queueCapacity);
            _minPQ = new MinPriorityQueue<Element>(queueCapacity);

            //_concurentQueue = new ConcurrentQueue<Element>(_q);
            //_concurentMinPQ = new ConcurrentQueue<Element>(_minPQ);
            _concurentQueue = _q;
            _concurentMinPQ = _minPQ;

            _processors = Environment.ProcessorCount;

            _capacity = queueCapacity;
            _processDone = 0;
        }
        
        public bool DoAction(Stream init, Stream archive, long bufferSize)
        {
            if (init.Length == 0)
                throw new ArgumentException("empty file");

            _bufferSize = bufferSize;

            var t1 = new Thread(() => WriteStreamSegmentsToQueue(init));
            t1.Name = "Read file";
            t1.Start();

            Thread[] threads = new Thread[_processors];
            for (int i = 0; i < _processors; i++)
            {
                threads[i] = new Thread(() => ProcessSegment(_concurentMinPQ));
                threads[i].Name = "Process " + i;
                threads[i].Start();
            }

            var t2 = new Thread(() => WriteProcessedSegmentsToStream(archive));
            t2.Name = "Write file";
            t2.Start();

            _completeEvent.WaitOne();

            return true;
        }

        public void ProcessSegment(IQueable<Element> minPQ)
        {
            while (!_readComplete || !_concurentQueue.IsEmpty)
            {
                if (minPQ.Size > _capacity) //MinPQ limit
                    continue;

                Element aux = _concurentQueue.Dequeue();

                if (aux != null)
                {
                    byte[] processedData = _gzip.DoWork(aux.Data);
                    minPQ.Enqueue(new Element { Number = aux.Number, Data = processedData });
                    aux = null;
                }
            }

            Interlocked.Increment(ref _processDone);
        }

        public void WriteProcessedSegmentsToStream(Stream toHdd)
        {
            int prevNumber = 0;
            while (_processDone < _processors || !_concurentMinPQ.IsEmpty)
            {
                Element min = _concurentMinPQ.PeekElement();

                //Ошибка min.Number - prevNumber придумать другие решение
                //Видимо ошибка синхронизации потоков
                if (min != null && min.Number - prevNumber == 1)
                {
#if DEBUG
                    Debug.WriteLine(string.Format("N:{0}", min.Number));
#endif
                    min = _concurentMinPQ.Dequeue();
                    toHdd.Write(min.Data, 0, min.Data.Length);
                    prevNumber = min.Number;
                    min = null;

                    if (_concurentQueue.IsEmpty && _concurentMinPQ.IsEmpty) //Queue limit
                        _stopReadEvent.Set();
                }
            }

            _completeEvent.Set();
        }

        public abstract void WriteStreamSegmentsToQueue(Stream fromHdd);
    }
}
