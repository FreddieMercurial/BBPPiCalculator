namespace BBP.FasterKVMiner.Test
{
    using BBP.FasterKVMiner;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class PiBufferTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var piBuffer = new PiBuffer(
                startingOffset: 10,
                maxCapacity: 30);
            var piBytes = piBuffer.GetPiSegment(
                minimum: 10,
                maximum: 20);
            var piBytesEarly = piBuffer.GetPiSegment(
                minimum: 0,
                maximum: 20);
            var piBytesLate = piBuffer.GetPiSegment(
                minimum: 0,
                maximum: 30);
            var piBytesPastBuffer = piBuffer.GetPiSegment(
                minimum: 0,
                maximum: 40);
        }
    }
}
