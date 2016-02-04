using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleZipUtility.Interfaces
{
    public interface IGzipAction
    {
        byte[] DoWork(byte[] buffer);
    }
}
