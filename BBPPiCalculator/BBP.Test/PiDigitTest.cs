using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BBP.Test;

/// <summary>
///     The unit testing is necessary since there is lots of parallelization happening.
///     During the optimization process we want to ensure that the data matches what
///     we expect.
/// </summary>
[TestClass]
public class PiDigitTest
{
    /// <summary>
    ///     Match the hex stream chars n to n+9
    /// </summary>
    [DataTestMethod]
    [DataRow(data1: 0, "243F6A8885")]
    [DataRow(data1: 10, "A308D31319")]
    [DataRow(data1: 20, "8A2E037073")]
    [DataRow(data1: 30, "44A4093822")]
    [DataRow(data1: 40, "299F31D008")]
    [DataRow(data1: 50, "2EFA98EC4E")]
    [DataRow(data1: 60, "6C89452821")]
    [DataRow(data1: 70, "E638D01377")]
    [DataRow(data1: 80, "BE5466CF34")]
    [DataRow(data1: 90, "E90C6CC0AC")]
    public void TestCalc(long n, string expected)
    {
        var result = BBPCalculator.Calculate(n: n);
        Assert.AreEqual(
            expected: expected,
            actual: result.HexDigits);
    }

    [DataTestMethod]
    [DataRow(data1: 0, 40, "243F6A8885A308D313198A2E03707344A4093822")]
    [DataRow(data1: 1, 38, "43F6A8885A308D313198A2E03707344A409382")]
    [DataRow(data1: 10, 30, "A308D313198A2E03707344A4093822")]
    public void TestPiChars(long n, int count, string expected)
    {
        var result = new string(value: BBPCalculator
            .PiChars(
                n: n,
                count: count)
            .Select(selector: c => c)
            .ToArray());
        Assert.AreEqual(expected: count, actual: result.Length);
        Assert.AreEqual(expected: expected, actual: result);
    }

    [DataTestMethod]
    [DataRow(data1: 0, 40, "243F6A8885A308D313198A2E03707344A4093822")]
    [DataRow(data1: 1, 38, "43F6A8885A308D313198A2E03707344A409382")]
    [DataRow(data1: 10, 30, "A308D313198A2E03707344A4093822")]
    public async Task TestPiCharsAsync(long n, int count, string expected)
    {
        var accumulator = new List<char>(capacity: count);
        await foreach (var c in BBPCalculator
                           .PiCharsAsync(
                               n: n,
                               count: count))
        {
            accumulator.Add(item: c);
        }

        var result = new string(value: accumulator.ToArray());
        Assert.AreEqual(expected: count, actual: result.Length);
        Assert.AreEqual(expected: expected, actual: result);
    }

    [DataTestMethod]
    [DataRow(data1: 0, 20,
        new byte[]
        {
            0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73, 0x44, 0xA4, 0x09, 0x38, 0x22,
        })]
    [DataRow(data1: 1, 19,
        new byte[] {0x43, 0xF6, 0xA8, 0x88, 0x5A, 0x30, 0x8D, 0x31, 0x31, 0x98, 0xA2, 0xE0, 0x37, 0x07, 0x34, 0x4A, 0x40, 0x93, 0x82})]
    [DataRow(data1: 10, 15, new byte[] {0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73, 0x44, 0xA4, 0x09, 0x38, 0x22})]
    public void TestPiBytes(long n, int count, byte[] expected)
    {
        var result = BBPCalculator
            .PiBytes(
                n: n,
                count: count)
            .ToArray();
        Assert.AreEqual(expected: count, actual: result.Length);
        Assert.IsTrue(condition: expected.SequenceEqual(second: result));
    }

    [DataTestMethod]
    [DataRow(data1: 0, 20,
        new byte[]
        {
            0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73, 0x44, 0xA4, 0x09, 0x38, 0x22,
        })]
    [DataRow(data1: 1, 19,
        new byte[] {0x43, 0xF6, 0xA8, 0x88, 0x5A, 0x30, 0x8D, 0x31, 0x31, 0x98, 0xA2, 0xE0, 0x37, 0x07, 0x34, 0x4A, 0x40, 0x93, 0x82})]
    [DataRow(data1: 10, 15, new byte[] {0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73, 0x44, 0xA4, 0x09, 0x38, 0x22})]
    public async Task TestPiBytesAsync(long n, int count, byte[] expected)
    {
        var accumulator = new List<byte>(capacity: count);
        await foreach (var b in BBPCalculator
                           .PiBytesAsync(
                               n: n,
                               count: count))
        {
            accumulator.Add(item: b);
        }

        var result = accumulator.ToArray();
        Assert.AreEqual(expected: count, actual: result.Length);
        Assert.IsTrue(condition: expected.SequenceEqual(second: result));
    }

    [TestMethod]
    public async Task TestRepeatCalls()
    {
        var nOffset = 0;
        // first digit + 9
        Assert.AreEqual(
            expected: "243F6A8885",
            actual: (await BBPCalculator.CalculateAsync(n: nOffset++).ConfigureAwait(continueOnCapturedContext: false)).HexDigits);
        // second digit + 9
        Assert.AreEqual(
            expected: "43F6A8885A",
            actual: (await BBPCalculator.CalculateAsync(n: nOffset++).ConfigureAwait(continueOnCapturedContext: false)).HexDigits);

        // 10th digit + 9
        nOffset = 10;
        Assert.AreEqual(
            expected: "A308D31319",
            actual: (await BBPCalculator.CalculateAsync(n: nOffset++).ConfigureAwait(continueOnCapturedContext: false)).HexDigits);
        // 11th digits + 9
        Assert.AreEqual(
            expected: "308D313198",
            actual: (await BBPCalculator.CalculateAsync(n: nOffset++).ConfigureAwait(continueOnCapturedContext: false)).HexDigits);
        // 12th digits + 8
        Assert.AreEqual(
            expected: "08D313198",
            actual: new string(value: BBPCalculator.PiChars(n: nOffset++, count: 9).ToArray()));

        // 31st digit + 9
        nOffset = 31;
        var accumulator = new List<char>(capacity: 10);
        await foreach (var c in BBPCalculator
                           .PiCharsAsync(
                               n: nOffset++,
                               count: 10))
        {
            accumulator.Add(item: c);
        }

        var result = new string(value: accumulator.ToArray());
        Assert.AreEqual(expected: "4A40938222", actual: result);
    }
}
