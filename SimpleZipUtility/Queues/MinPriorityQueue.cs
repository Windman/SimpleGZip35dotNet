using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZipUtility.Queues
{
    public class MinPriorityQueue<T> : IQueable<T>
    {
        private T[] pq;
        private int N;
        private int _capacity;

        public bool IsEmpty { get { return N == 0; } }

        public bool IsActive
        {
            get
            {
                if (Size < _capacity)
                    return true;

                return false;
            }
        }

        public int Size { get { return N; } }

        public MinPriorityQueue(int capacity)
        {
            pq = new T[capacity + 1];
            N = 0;
            _capacity = capacity;
        }

        public void Enqueue(T e)
        {
            if (N == pq.Length - 1) 
                resize(2 * pq.Length);
            pq[++N] = e;
            Swim(N);
        }

        public T Dequeue()
        {
            if (IsEmpty) throw new Exception("Priority queue is empty");
            
            Exchange(1, N);
            
            T min = pq[N--];
            
            Sink(1);
            
            pq[N + 1] = default(T);
            
            if ((N > 0) && (N == (pq.Length - 1) / 4)) 
                resize(pq.Length / 2);
            
            return min;
        }

        public T PeekElement()
        {
            return pq[1];
        }

        private void resize(int capacity)
        {
            T[] temp = new T[capacity];
            for (int i = 1; i <= N; i++)
            {
                temp[i] = pq[i];
            }
            pq = temp;
        }

        private void Swim(int k)
        {
            while (k > 1 && greater(k / 2, k))
            {
                Exchange(k, k / 2);
                k = k / 2;
            }
        }

        private void Sink(int k)
        {
            while (2 * k <= N)
            {
                int j = 2 * k;
                if (j < N && greater(j, j + 1)) j++;
                if (!greater(k, j)) break;
                Exchange(k, j);
                k = j;
            }
        }

        #region Helper functions

        private bool greater(int i, int j)
        {
            return ((IComparable<T>)pq[i]).CompareTo(pq[j]) > 0;
        }

        private void Exchange(int i, int j)
        {
            T swap = pq[i];
            pq[i] = pq[j];
            pq[j] = swap;
        }

        #endregion


       
    }
}
