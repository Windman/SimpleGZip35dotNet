using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleZipUtility.Queues;
using SimpleZipUtility;

namespace GZipUnitTest
{
    [TestClass]
    public class MinPriorityQueueTests
    {
        [TestMethod]
        public void MinPriorityQueue_Enqueue()
        {
            int capacity = 5;

            var q = new MinPriorityQueue<Element>(capacity);

            for (int i = capacity; i > 0; i--)
            {
                q.Enqueue(new Element { Number = i, Data = new byte[i]});
            }

            Assert.IsTrue(q.Size == capacity);

            for (int i = 1; i <= capacity; i++)
            {
                var e = q.Dequeue();
                Assert.IsTrue(i == e.Number);
            }
        }
    }
}
