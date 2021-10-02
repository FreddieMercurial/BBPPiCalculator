namespace BBP.Test
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using BBP;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    /// <summary>
    /// The unit testing is necessary since there is lots of parallelization happening.
    /// During the optimization process we want to ensure that the data matches what
    /// we expect.
    /// </summary>
    [TestClass]
    public class PiDigitTest
    {
        /// <summary>
        /// Match the hex stream chars n to n+9
        /// </summary>
        [DataTestMethod]
        [DataRow(0, "243F6A8885")]
        [DataRow(10, "A308D31319")]
        [DataRow(20, "8A2E037073")]
        [DataRow(30, "44A4093822")]
        [DataRow(40, "299F31D008")]
        [DataRow(50, "2EFA98EC4E")]
        [DataRow(60, "6C89452821")]
        [DataRow(70, "E638D01377")]
        [DataRow(80, "BE5466CF34")]
        [DataRow(90, "E90C6CC0AC")]
        public void TestCalc(long n, string expected)
        {
            PiDigit pd = new PiDigit(nOffset: 0);
            BBPResult result = pd.Calc(n: n);
            Assert.AreEqual(
                expected: expected,
                actual: result.HexDigits);

            // test separate instance with same nethod
            PiDigit pd2 = new PiDigit(nOffset: 0);
            BBPResult result2 = pd2.Calc(n: n);
            Assert.AreEqual(
                expected: expected,
                actual: result.HexDigits);

            // test separate instance with alternate method
            PiDigit pd3 = new PiDigit(nOffset: n);
            BBPResult result3 = pd3.Next();
            Assert.AreEqual(
                expected: expected,
                actual: result.HexDigits);

            // test separate instance with alternate, static method
            BBPResult result4 = PiDigit.Calculate(n: n);
            Assert.AreEqual(
                expected: expected,
                actual: result4.HexDigits);
        }

        [DataTestMethod]
        [DataRow(0, 40, "243F6A8885A308D313198A2E03707344A4093822")]
        [DataRow(1, 38, "43F6A8885A308D313198A2E03707344A409382")]
        [DataRow(10, 30, "A308D313198A2E03707344A4093822")]
        public void TestPiBytes(long n, int count, string expected)
        {
            PiDigit pd = new PiDigit();
            var result = new string(pd
                .PiBytes(
                    n: n,
                    count: count)
                .Select(c => (char)c)
                .ToArray());
            Assert.AreEqual(expected: count, result.Length);
            Assert.AreEqual(expected: expected, actual: result);
        }

        [DataTestMethod]
        [DataRow(0, 40, "243F6A8885A308D313198A2E03707344A4093822")]
        [DataRow(1, 38, "43F6A8885A308D313198A2E03707344A409382")]
        [DataRow(10, 30, "A308D313198A2E03707344A4093822")]
        public async Task TestPiBytesAsync(long n, int count, string expected)
        {
            PiDigit pd = new PiDigit();
            List<char> accumulator = new List<char>();
            await foreach(var c in pd
                .PiBytesAsync(
                    n: n,
                    count: count))
            {
                accumulator.Add(c);
            }

            var result = new string(accumulator.ToArray());
            Assert.AreEqual(expected: count, result.Length);
            Assert.AreEqual(expected: expected, actual: result);
        }

        [TestMethod]
        public async Task TestRepeatCalls()
        {
            PiDigit pd = new PiDigit();
            // first digit + 9
            Assert.AreEqual(
                expected: "243F6A8885",
                actual: pd.Next().HexDigits);
            // second digit + 9
            Assert.AreEqual(
                expected: "43F6A8885A",
                actual: pd.Next().HexDigits);

            // 10th digit + 9
            Assert.AreEqual(
                expected: "A308D31319",
                actual: pd.Calc(n: 10).HexDigits);
            // 11th digits + 9
            Assert.AreEqual(
                expected: "308D313198",
                actual: pd.Next().HexDigits);
            // 12th digits + 8
            Assert.AreEqual(
                expected: "08D313198",
                actual: new string(pd.PiBytes(n: 12, count: 9).ToArray()));

            // 30th digit + 9
            List<char> accumulator = new List<char>();
            await foreach (var c in pd
                .PiBytesAsync(
                    n: -1,
                    count: 10))
            {
                accumulator.Add(c);
            }
            var result = new string(accumulator.ToArray());
            Assert.AreEqual(expected: "4A40938222", actual: result);
        }
    }
}
