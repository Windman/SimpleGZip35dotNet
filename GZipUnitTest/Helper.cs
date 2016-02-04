using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GZipUnitTest
{
    public static class Helper
    {
        public static byte[] GZIP_HEADER_BYTES = new byte[] { 0x1f, 0x8b, 8, 0, 0, 0, 0, 0, 4, 0 };

        public static string GetMd5Hash(MD5 md5Hash, byte[] input)
        {
            byte[] data = md5Hash.ComputeHash(input);
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        public static bool VerifyMd5Hash(MD5 md5Hash, byte[] input, string hash)
        {
            string hashOfInput = GetMd5Hash(md5Hash, input);
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            
            if (0 == comparer.Compare(hashOfInput, hash))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static byte[] CreateInMemmoryTestFile(long fileSize)
        {
            byte[] result = null;
            try
            {
                result = new byte[fileSize];

                Random rand = new Random();

                for (long i = 0; i < fileSize; i++)
                {
                    result[i] = (byte)(rand.Next() % 256);
                }

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static void CreateTestFile(string path, long sizeMbBytes)
        {
            Console.WriteLine("Creating file of size {0} Mb", sizeMbBytes);
            try
            {
                long fileSize = sizeMbBytes * 1024 * 1024;
                Random rand = new Random();
                int buffer = 2 << 18;
                byte nextByte;

                using (FileStream fs = new FileStream(
                    path,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    buffer,
                    FileOptions.None))
                {
                    for (long j = 0; j < fileSize; j++)
                    {
                        nextByte = (byte)(rand.Next() % 256);
                        fs.WriteByte(nextByte);
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
