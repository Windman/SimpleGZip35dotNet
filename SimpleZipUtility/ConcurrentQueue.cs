using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SimpleZipUtility
{
    public class ConcurrentQueue<T>
    {
        Queue<T> _sharedQueue;
        internal ReaderWriterLockSlim _rw;

        private             int _capacity;
        private volatile    int _size;

        public ConcurrentQueue(int capacity)
        {
            _sharedQueue = new Queue<T>();
            _rw = new ReaderWriterLockSlim();
            _size = 0;
            _capacity = capacity;
        }

        public void Enqueue(T e)
        {
            try
            {
                _rw.EnterWriteLock();
                _sharedQueue.Enqueue(e);
                _size++;
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
                
                if (!IsEmpty() && _size < _capacity)
                {
                    try
                    {
                        _rw.EnterReadLock();
                        aux = _sharedQueue.Dequeue();
                        _size--;
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
            if (_size == 0)
                return true;
            
            return false;
        }
    }
}
