namespace TenderBaseTest
{
    using System;
    using NUnit.Framework;
    using NAssert = NUnit.Framework.Assert;
    using TenderBase;
    using TenderBaseImpl;
    
    [TestFixture]
    public class ByteBufferTest
    {
        public ByteBuffer bb;

        public void AssertInitialUsed(ByteBuffer bb, int used)
        {
            NAssert.AreEqual(used, bb.used);
            NAssert.AreEqual(ByteBuffer.INITIAL_SIZE, bb.arr.Length);
        }

        [SetUp]
        public void Init()
        {
            bb = new ByteBuffer();
        }

        public void TestExtendLessThanOrEqualToInitialSize(int size)
        {
            bb.Extend(size);
            AssertInitialUsed(bb, size);
        }

        [Test]
        public void TestExtend0()
        {
            AssertInitialUsed(bb, 0);
        }

        [Test]
        public void TestExtend1()
        {
            TestExtendLessThanOrEqualToInitialSize(0);
        }

        [Test]
        public void TestExtend2()
        {
            TestExtendLessThanOrEqualToInitialSize(ByteBuffer.INITIAL_SIZE / 2);
        }

        [Test]
        public void TestExtend3()
        {
            TestExtendLessThanOrEqualToInitialSize(ByteBuffer.INITIAL_SIZE - 1);
        }

        [Test]
        public void TestExtend4()
        {
            TestExtendLessThanOrEqualToInitialSize(ByteBuffer.INITIAL_SIZE);
        }

        [Test]
        public void TestExtend5()
        {
            bb.Extend(ByteBuffer.INITIAL_SIZE + 1);
            NAssert.AreEqual(ByteBuffer.INITIAL_SIZE * 2, bb.arr.Length);
        }

        [Test]
        public void TestExtend6()
        {
            int ns = bb.arr.Length * 2 + 1;
            bb.Extend(ns);
            NAssert.AreEqual(ns, bb.arr.Length);
        }
    }
}

