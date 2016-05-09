using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZipUtility.Queues
{
    public delegate void QueueOverflowEventHandler(object sender, ElementEventArgs args);
    public delegate void EmptyQueueEventHandler(object sender);

    public class ElementEventArgs
    {
        public ElementEventArgs(Element e) { StopElement = e; }
        public Element StopElement {get; private set;} 
    }

    public class SimpleQueue : Queue<Element>, IQueable<Element>
    {
        private Object stub = new Object();

        public event QueueOverflowEventHandler QueueOverflow;
        public event EmptyQueueEventHandler EmptyQueue;

        private int _capacity;

        public SimpleQueue(int capacity)
        {
            _capacity = capacity;
        }

        public void Enqueue(Element item)
        {
            if (_capacity > Size)
                base.Enqueue(item);
            else RaiseQueueOverflow(new ElementEventArgs(item));
        }

        public Element Dequeue()
        {
            lock (stub)
            {
                if (IsEmpty)
                {
                    RaiseEmptyQueue();
                    return null;
                }
                else
                    return base.Dequeue();
            }
        }

        public Element PeekElement()
        {
            lock (stub)
            {
                return base.Peek();
            }
        }

        public bool IsEmpty
        {
            get
            {
                if (Count == 0)
                    return true;

                return false;
            }
        }

        public int Size
        {
            get { return Count; }
        }

        protected virtual void RaiseQueueOverflow(ElementEventArgs arg)
        {
            if (QueueOverflow != null)
                QueueOverflow(this, arg);
        }

        private void RaiseEmptyQueue()
        {
            if (EmptyQueue != null)
                EmptyQueue(this);
        }
    }
}
