using SimpleZipUtility.Interfaces;
using SimpleZipUtility.Queues;
using System;
using System.Collections.Generic;
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
        
        internal ManualResetEvent _completeEvent = new ManualResetEvent(false);
        internal ManualResetEvent _stopReadEvent = new ManualResetEvent(false);
        
        internal long _totalBytesRead;
        internal long _bufferSize;

        internal volatile bool _readComplete = false;
        internal IGzipAction _gzip;

        internal Concurrent<Element> _concurentQueue;
        internal Concurrent<Element> _concurentMinPQ;

        private IQueable<Element> _q;
        private IQueable<Element> _minPQ;
        
        public BaseEngine(IGzipAction gzip, int queueCapacity)
        {
            _gzip = gzip;
            _q = new SimpleQueue();
            _concurentQueue = new Concurrent<Element>(_q);
            _minPQ = new MinPriorityQueue<Element>(queueCapacity);
            _concurentMinPQ = new Concurrent<Element>(_minPQ);
        }

        public abstract void WriteStreamSegmentsToQueue(Stream fromHdd);
        public abstract void WriteProcessedSegmentsToStream(Stream toHdd);
    }
}
