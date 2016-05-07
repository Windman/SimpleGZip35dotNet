using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SimpleZipUtility.Queues;

namespace SimpleZipUtility.Queues
{
    public interface IQueable<T>
    {
        bool IsEmpty { get; }
        int Size { get; }
        void Enqueue(T e);
        T Dequeue();
        T PeekElement();
        event QueueOverflowEventHandler QueueOverflow;
        event EmptyQueueEventHandler EmptyQueue;
    }
}
