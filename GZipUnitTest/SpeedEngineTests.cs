using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using SimpleZipUtility;
using System.IO;
using SimpleZipUtility.Interfaces;

namespace GZipUnitTest
{
    [TestClass]
    public class SpeedEngineTests
    {
        IArchivator engine;
        byte[] file;

        [TestInitialize]
        public void Init()
        {
            int fileSize = 256; //20 * 1024 * 1024;
            file = Helper.CreateInMemmoryTestFile(fileSize);
        }

        [TestMethod]
        public void Speed_CompressDeCompression_FileSizeGreaterThanBuffer()
        {
            int bufferSize = 50; //3 * 1024 * 1024; ;

            CompressDeCompress(bufferSize);
        }

        [TestMethod]
        public void Speed_CompressDeCompression_FileSizeEualBufferSize()
        {
            int bufferSize = 20 * 1024 * 1024; ;

            CompressDeCompress(bufferSize);
        }


        private void CompressDeCompress(long bufferSize)
        {
            byte[] archivedFile;

            string hash;

            using (MD5 md5Hash = MD5.Create())
            {
                hash = Helper.GetMd5Hash(md5Hash, file);
            }

            var engine = new EngineCompress(new GZipCompress());
            using (MemoryStream original = new MemoryStream(file))
            {
                using (MemoryStream archive = new MemoryStream())
                {
                    engine.DoAction(original, archive, bufferSize);
                    archivedFile = archive.ToArray();
                }
            }

            byte[] dearchivedFile = null;
            using (MemoryStream archive = new MemoryStream(archivedFile))
            {
                using (MemoryStream original = new MemoryStream())
                {
                    EngineDecompress engine2 = new EngineDecompress(new GZipDecompress());
                    engine2.DoAction(archive, original, bufferSize);
                    dearchivedFile = original.ToArray();
                }
            }

            using (MD5 md5Hash = MD5.Create())
            {
                Assert.IsTrue(Helper.VerifyMd5Hash(md5Hash, dearchivedFile, hash));
            }
        }

        [TestMethod]
        public void Find_Segment()
        {
            var engine = new EngineDecompress(new GZipDecompress());
            byte[] buffer = new byte[60];
            Array.Copy(Helper.GZIP_HEADER_BYTES, 0, buffer, 10, Helper.GZIP_HEADER_BYTES.Length);
            Array.Copy(Helper.GZIP_HEADER_BYTES, 0, buffer, 50, Helper.GZIP_HEADER_BYTES.Length);

            byte[] segment = null;
            Assert.IsTrue(engine.FindSegmentStartAndLastIndex(buffer, ref segment) == true);
            Assert.IsTrue(segment.Length == 40);

            //LastPeace
            byte[] buffer2 = new byte[60];
            Array.Copy(Helper.GZIP_HEADER_BYTES, 0, buffer2, 0, Helper.GZIP_HEADER_BYTES.Length);
                        
            Assert.IsTrue(engine.FindSegmentStartAndLastIndex(buffer2, ref segment) == false);
            Assert.IsTrue(segment.Length == buffer2.Length);
        }
    }
}
