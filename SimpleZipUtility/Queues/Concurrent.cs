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
                //if (_queue.IsActive)
                //{
                //    _queue.Enqueue(e);
                //}
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

        public bool IsEmpty() 
        { 
            try
            {
                _rw.EnterUpgradeableReadLock();
                return _queue.IsEmpty;
            }
            finally
            {
                _rw.ExitUpgradeableReadLock();
            }
        }

        public int Size()
        {
            try
            {
                _rw.EnterUpgradeableReadLock();
                return _queue.Size;
            }
            finally
            {
                _rw.ExitUpgradeableReadLock();
            }
        } 
    }
}
