using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BBP.FasterKVMiner.Test;

[TestClass]
public class PiBufferTests
{
    private MockRepository mockRepository;


    [TestInitialize]
    public void TestInitialize()
    {
        mockRepository = new MockRepository(defaultBehavior: MockBehavior.Strict);
    }

    private PiBuffer CreatePiBuffer()
    {
        return new PiBuffer(
            startingCharOffset: 10,
            maxByteCapacity: 20);
    }

    [TestMethod]
    public void GetPiSegment_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        var piBuffer = CreatePiBuffer();
        long minimum = 10;
        long maximum = 20;

        // Act
        var piBytes = piBuffer.GetPiSegment(
            minimum: minimum,
            maximum: maximum);

        var piBytesTestPrepend = piBuffer.GetPiSegment(
            minimum: 0,
            maximum: 20);

        var piBytesTestAppend = piBuffer.GetPiSegment(
            minimum: 0,
            maximum: 30);

        // TODO: it's possible we might be able to screw things up by moving to an odd minimum- idiot proof?
        // starting at 10 - 20 and moving to 11 to 21 or something, or 11 - 21 and moving to 10-22
        var piBytesTestAppendTwice = piBuffer.GetPiSegment(
            minimum: 0,
            maximum: 40);

        // Assert
        Assert.IsTrue(condition: piBytes.SequenceEqual(second: new byte[] {0xA3, 0x08, 0xD3, 0x13, 0x19}));
        Assert.IsTrue(
            condition: piBytesTestPrepend.SequenceEqual(second: new byte[] {0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19}));
        Assert.IsTrue(condition: piBytesTestAppend.SequenceEqual(second: new byte[]
        {
            0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73,
        }));
        Assert.IsTrue(condition: piBytesTestAppendTwice.SequenceEqual(second: new byte[]
        {
            0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73, 0x44, 0xA4, 0x09, 0x38, 0x22,
        }));

        Assert.ThrowsException<ArgumentException>(action: () =>
        {
            var piBytesTestTooBig = piBuffer.GetPiSegment(
                minimum: 0,
                maximum: 42);
        });

        mockRepository.VerifyAll();
    }

    [TestMethod]
    public void Dispose_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        var piBuffer = CreatePiBuffer();

        // Act
        piBuffer.Dispose();

        // Assert
        Assert.Fail();
        mockRepository.VerifyAll();
    }
}
