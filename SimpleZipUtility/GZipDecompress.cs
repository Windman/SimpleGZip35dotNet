using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using SimpleZipUtility.Interfaces;
using SimpleZipUtility.UtilityExtensions;

namespace SimpleZipUtility
{
    public class GZipDecompress : IGzipAction
    {
        public byte[] DoWork(byte[] buffer)
        {
            int readBytes = 0;
            byte[] newbuffer = new byte[buffer.Length];

            using(MemoryStream output = new MemoryStream())
            {
                using (var ms = new MemoryStream(buffer))
                {
                    using (var gzip = new GZipStream(ms, CompressionMode.Decompress))
                    {
                        while ((readBytes = gzip.Read(newbuffer, 0, newbuffer.Length)) > 0)
                        {
                            if (readBytes < newbuffer.Length)
                                newbuffer = newbuffer.TruncateBuffer(readBytes);
                            output.Write(newbuffer, 0, newbuffer.Length);
                        }
                    }
                }
                return output.ToArray();
            }
        }
    }
}
