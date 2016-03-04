using SimpleZipUtility.Interfaces;
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
        
        internal ManualResetEvent _mainEvent = new ManualResetEvent(false);
        internal ManualResetEvent _completeEvent = new ManualResetEvent(false);

        internal long _totalBytesRead;
        internal long _bufferSize;

        internal volatile bool _readComplete = false;
        internal IGzipAction _gzip;

        internal ConcurrentQueue<byte[]> _sharedQueue;

        public BaseEngine(IGzipAction gzip, int queueCapacity)
        {
            _gzip = gzip;
            _sharedQueue = new ConcurrentQueue<byte[]>(queueCapacity);
        }

        public abstract void WriteStreamSegmentsToSharedQueue(Stream init);

        public void DequeueSegmentToStream(Stream stream, long totalBytes)
        {
            _mainEvent.WaitOne();
            long processedBytes = 0;

            while (true)
            {
                byte[] aux = _sharedQueue.Dequeue();

                if (aux != null)
                {
                    byte[] processedData = _gzip.DoWork(aux);
                    stream.Write(processedData, 0, processedData.Length);
                    processedBytes += aux.Length;
                }

                if (!_readComplete)
                    _mainEvent.WaitOne();

                if (_readComplete && _sharedQueue.IsEmpty())
                    break;
            }
            _completeEvent.Set();
        }
    }
}
