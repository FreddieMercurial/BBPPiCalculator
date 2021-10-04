namespace BBP.FasterKVMiner.Test
{
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

            var piBytesEarly = piBuffer.GetPiSegment(
                minimum: 0,
                maximum: 20);
            Assert.IsTrue(piBytesEarly.SequenceEqual(new byte[] { 0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19 }));

            var piBytesLate = piBuffer.GetPiSegment(
                minimum: 0,
                maximum: 30);
            Assert.IsTrue(piBytesLate.SequenceEqual(new byte[] { 0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73 }));

            var piBytesPastBuffer = piBuffer.GetPiSegment(
                minimum: 0,
                maximum: 40);
            Assert.IsTrue(piBytesPastBuffer.SequenceEqual(new byte[] { 0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73, 0x44, 0xA4, 0x09, 0x38, 0x22 }));
        }
    }
}
