using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SimpleZipUtility.Queues
{
    public class Concurrent<T>
    {
        private ReaderWriterLockSlim _rw;
        private IQueable<T> _queue;

        public bool IsEmpty { get { return _isEmpty; } }
        public int Size { get { return _size; } }

        private bool _isEmpty;
        private int _size;

        public Concurrent(IQueable<T> queue)
        {
            _rw = new ReaderWriterLockSlim();
            _queue = queue;
        }

        public void Enqueue(T e)
        {
            try
            {
                _rw.EnterWriteLock();
                _queue.Enqueue(e);
                
                _size = _queue.Size;
                _isEmpty = _queue.IsEmpty;
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        public T Dequeue()
        {
            T aux = default(T);
            try
            {
                _rw.EnterUpgradeableReadLock();

                if (!_queue.IsEmpty)
                {
                    try
                    {
                        _rw.EnterReadLock();
                        aux = _queue.Dequeue();
                        
                        _size = _queue.Size;
                        _isEmpty = _queue.IsEmpty;
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

            return aux;
        }

        public T Peak()
        {
            T aux = default(T);
            try
            {
                _rw.EnterUpgradeableReadLock();

                if (!_queue.IsEmpty)
                {
                    try
                    {
                        _rw.EnterReadLock();
                        aux = _queue.PeekElement();
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

            return aux;
        }
    }
}
