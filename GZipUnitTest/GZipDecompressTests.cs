using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using SimpleZipUtility;
using System.IO;

namespace GZipUnitTest
{
    [TestClass]
    public class GZipDecompressTests
    {
        private byte[] file; 

        [TestInitialize]
        public void Init()
        {
            int filesizeMb = 20;
            file = Helper.CreateInMemmoryTestFile(filesizeMb);
        }

        [TestMethod]
        public void Decompress()
        {
            string hash;
            byte[] archivedFile;
            long bufferSize = 20 * 1024 * 1024;

            using (MD5 md5Hash = MD5.Create())
            {
                hash = Helper.GetMd5Hash(md5Hash, file);
            }

            var engine = new EngineCompress(new GZipCompress(), 100);
            using (MemoryStream original = new MemoryStream(file))
            {
                using (MemoryStream archive = new MemoryStream())
                {
                    engine.DoAction(original, archive, bufferSize);
                    archivedFile = archive.ToArray();
                }
            }

            GZipDecompress gzip = new GZipDecompress();
            var originalFile = gzip.DoWork(archivedFile);

            using (MD5 md5Hash = MD5.Create())
            {
                hash = Helper.GetMd5Hash(md5Hash, originalFile);

                Assert.IsTrue(Helper.VerifyMd5Hash(md5Hash, file, hash));
            }
        }
    }
}
