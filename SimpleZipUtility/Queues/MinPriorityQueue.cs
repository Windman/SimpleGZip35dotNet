using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZipUtility.Queues
{
    public class MinPriorityQueue<T> : IQueable<T>
    {
        private Object stub = new Object();

        private T[] pq;
        private int N;

        public bool IsEmpty { get { return N == 0; } }
                
        public int Size { get { return N; } }

        public MinPriorityQueue(int capacity)
        {
            pq = new T[capacity + 1];
            N = 0;
        }

        public void Enqueue(T e)
        {
            lock (stub)
            {
                if (N == pq.Length - 1)
                    Resize(2 * pq.Length);
                pq[++N] = e;
                Swim(N);
            }
        }

        public T Dequeue()
        {
            lock(stub)
            {
                if (IsEmpty) throw new Exception("Priority queue is empty");

                Exchange(1, N);

                T min = pq[N--];

                Sink(1);

                pq[N + 1] = default(T);

                if ((N > 0) && (N == (pq.Length - 1) / 4))
                    Resize(pq.Length / 2);

                return min;
            }
        }

        public T PeekElement()
        {
            lock(stub)
            {
                return pq[1];
            }
        }

        private void Resize(int capacity)
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
            while (k > 1 && Greater(k / 2, k))
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
                if (j < N && Greater(j, j + 1)) j++;
                if (!Greater(k, j)) break;
                Exchange(k, j);
                k = j;
            }
        }

        #region Helper functions

        private bool Greater(int i, int j)
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
        
        public event QueueOverflowEventHandler QueueOverflow;


        public event EmptyQueueEventHandler EmptyQueue;
    }
}
