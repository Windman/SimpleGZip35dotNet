using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using SimpleZipUtility.Interfaces;
using SimpleZipUtility.UtilityExtensions;

namespace SimpleZipUtility
{
    public class EngineCompress: BaseEngine, IArchivator
    {
        public EngineCompress(IGzipAction gzip)
            : base(gzip)
        {
                
        }

        public bool DoAction(Stream init, Stream archive, long bufferSize)
        {
            if (init.Length == 0)
                throw new ArgumentException("empty file");

            _bufferSize = bufferSize;
            
            var t1 = new Thread(() => WriteStreamSegmentsToSharedQueue(init)) { IsBackground = true };
            var t2 = new Thread(() => DequeueSegmentToStream(archive, init.Length));
            
            t1.Start();
            t2.Start();

            _completeEvent.WaitOne();
            return true;
        }

        public override void WriteStreamSegmentsToSharedQueue(Stream init)
        {
            long bytesRead = 0;
            byte[] buffer = new byte[_bufferSize];
            while ((bytesRead = init.Read(buffer, 0, buffer.Length)) > 0)
            {
                if (bytesRead < buffer.Length)
                    buffer = buffer.TruncateBuffer(bytesRead);
                
                try
                {
                    _rw.EnterWriteLock();
                    byte[] copy = new byte[buffer.Length];
                    Array.Copy(buffer, copy, buffer.Length);
                    _sharedQueue.Enqueue(copy);
                }
                finally
                {
                    _rw.ExitWriteLock();
                }

                _totalBytesRead += bytesRead;
                _mainEvent.Set();
            }
            _readComplete = true;
            _mainEvent.Set();
        }
    }
}
