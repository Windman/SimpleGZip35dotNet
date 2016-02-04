using SimpleZipUtility.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SimpleZipUtility
{
    public class GZipCompress : IGzipAction
    {
        public byte[] DoWork(byte[] buffer)
        {
            byte[] compressed = null;
            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Compress))
                {
                    gzip.Write(buffer, 0, buffer.Length);
                    gzip.Close();
                }

                compressed = ms.ToArray();
            }

            return compressed;
        }
    }
}
