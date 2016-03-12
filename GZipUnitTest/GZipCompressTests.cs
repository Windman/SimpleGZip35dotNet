using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleZipUtility;
using System.IO;

namespace GZipUnitTest
{
    [TestClass]
    public class GZipCompressTests
    {
        private byte[] file; 

        [TestInitialize]
        public void Init()
        {
            int fileSizeBytes = 20 * 1024 * 1024;
            file = Helper.CreateInMemmoryTestFile(fileSizeBytes);
        }

        [TestMethod]
        public void Compress_Test_CAPACITY_TOTAL_SEGMENTS_5_20()
        {
            long bufferSize = 1 * 1024 * 1024;
            int capacity = 5;

            byte[] archivedFile;

            var engine = new CompressEngine(new GZipCompress(), capacity);
            using (MemoryStream original = new MemoryStream(file))
            {
                using (MemoryStream archive = new MemoryStream())
                {
                    engine.DoAction(original, archive, bufferSize);
                    archivedFile = archive.ToArray();
                }
            }
        }
    }
}
