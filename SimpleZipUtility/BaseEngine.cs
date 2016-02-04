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
        public long ToralBytesRead
        {
            get { return _totalBytesRead; }
        }
        
        internal ReaderWriterLockSlim _rw = new ReaderWriterLockSlim();
        internal ManualResetEvent _mainEvent = new ManualResetEvent(false);
        internal ManualResetEvent _completeEvent = new ManualResetEvent(false);

        internal Queue<byte[]> _sharedQueue = new Queue<byte[]>();

        internal long _totalBytesRead;
        internal long _bufferSize;

        internal volatile bool _readComplete = false;
        internal IGzipAction _gzip;

        public BaseEngine(IGzipAction gzip)
        {
            _gzip = gzip;    
        }

        public abstract void WriteStreamSegmentsToSharedQueue(Stream init);

        public void DequeueSegmentToStream(Stream stream, long totalBytes)
        {
            _mainEvent.WaitOne();
            long processedBytes = 0;

            while (true)
            {
                try
                {
                    _rw.EnterUpgradeableReadLock();
                    if (_sharedQueue.Count > 0)
                    {
                        try
                        {
                            _rw.EnterReadLock();
                            byte[] aux = _sharedQueue.Dequeue();

                            if (aux != null)
                            {
                                byte[] processedData = _gzip.DoWork(aux);
                                stream.Write(processedData, 0, processedData.Length);
                                processedBytes += aux.Length;
                            }
                        }
                        finally
                        {
                            _rw.ExitReadLock();
                        }
                    }
                }
                finally
                {
                    _rw.ExitUpgradeableReadLock();
                }

                if (!_readComplete)
                    _mainEvent.WaitOne();

                if (_readComplete && _sharedQueue.Count == 0)
                    break;
            }
            _completeEvent.Set();
        }
    }
}
