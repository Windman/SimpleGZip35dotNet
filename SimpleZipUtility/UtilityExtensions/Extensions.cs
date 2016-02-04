using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleZipUtility.UtilityExtensions
{
    public static class Extensions
    {
        public static void CopyStream(this Stream input, Stream output, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
        }

        public static byte[] TruncateBuffer(this byte[] buffer, long bytesRead)
        {
            byte[] newBuffer = new byte[bytesRead];
            Array.Copy(buffer, newBuffer, bytesRead);
            return newBuffer;
        }

        public static byte[] ResizeArray(this byte[] source, long size)
        {
            byte[] destination = new byte[source.Length + size];
            Array.Copy(source, destination, source.Length);
            return destination;
        }
    }
}
