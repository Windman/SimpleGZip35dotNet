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
        public CompressEngine(IGzipAction gzip, int queueCapacity)
            : base(gzip, queueCapacity)
        {
            _q.QueueOverflow += QueueOverflow;
            _q.EmptyQueue += EmptyQueue;
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
                copy = null;

                _totalBytesRead += bytesRead;
#if DEBUG
                //Debug.WriteLine(string.Format("QueueSize: {0}, MinPQSize: {1}, TotalKBytes Read: {2}", _concurentQueue.Size, _concurentMinPQ.Size, _totalBytesRead/1024));
#endif
            }
            _readComplete = true;
        }

        private void QueueOverflow(object sender, ElementEventArgs args)
        {
            Debug.WriteLine("Stop read from file");
            _stopReadEvent.WaitOne();
            _concurentQueue.Enqueue(args.StopElement);
        }
                
        private void EmptyQueue(object sender)
        {
            _stopReadEvent.Set();
            Debug.WriteLine("Start read from file");
        }
    }
}
