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

        bool IQueable<Element>.IsEmpty
        {
            get
            {
                if (Count == 0)
                    return true;

                return false;
            }
        }

        bool IQueable<Element>.IsActive
        {
            get
            {
                if (Count < _capacity)
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
