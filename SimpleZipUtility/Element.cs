using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZipUtility
{
    public class Element: IComparable<Element>
    {
        public int Number { get; set; }
        public byte[] Data { get; set; }
        
        public int CompareTo(Element that)
        {
            if (that.Number > this.Number)
                return -1;
            else if (that.Number < this.Number)
                return 1;
            else return 0;
        }
    }
}
