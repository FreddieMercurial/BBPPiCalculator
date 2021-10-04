namespace BBP.FasterKVMiner.Test
{
    using System;
    using System.Linq;
    using BBP.FasterKVMiner;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PiBufferTest
    {
        [TestMethod]
        public void TestPiBuffer()
        {
            var piBuffer = new PiBuffer(
                startingCharOffset: 10,
                maxByteCapacity: 20);

            var piBytes = piBuffer.GetPiSegment(
                minimum: 10,
                maximum: 20);
            Assert.IsTrue(piBytes.SequenceEqual(new byte[] { 0xA3, 0x08, 0xD3, 0x13, 0x19 }));

            var piBytesTestPrepend = piBuffer.GetPiSegment(
                minimum: 0,
                maximum: 20);
            Assert.IsTrue(piBytesTestPrepend.SequenceEqual(new byte[] { 0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19 }));

            var piBytesTestAppend = piBuffer.GetPiSegment(
                minimum: 0,
                maximum: 30);
            Assert.IsTrue(piBytesTestAppend.SequenceEqual(new byte[] { 0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73 }));

            // TODO: it's possible we might be able to screw things up by moving to an odd minimum- idiot proof?
            // starting at 10 - 20 and moving to 11 to 21 or something, or 11 - 21 and moving to 10-22
            var piBytesTestAppendTwice = piBuffer.GetPiSegment(
                minimum: 0,
                maximum: 40);
            Assert.IsTrue(piBytesTestAppendTwice.SequenceEqual(new byte[] { 0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73, 0x44, 0xA4, 0x09, 0x38, 0x22 }));

            Assert.ThrowsException<ArgumentException>(() =>
            {
                var piBytesTestTooBig = piBuffer.GetPiSegment(
                minimum: 0,
                maximum: 42);
            });
        }
    }
}
