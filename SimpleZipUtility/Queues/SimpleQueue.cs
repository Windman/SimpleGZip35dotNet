using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZipUtility.Queues
{
    public class SimpleQueue: Queue<Element>, IQueable<Element> 
    {
        int _capacity;

        public SimpleQueue(int capacity)
        {
            _capacity = capacity;
        }

        public Element PeekElement()
        {
            return base.Peek();
        }
        
        bool IQueable<Element>.IsEmpty
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
    }
}
