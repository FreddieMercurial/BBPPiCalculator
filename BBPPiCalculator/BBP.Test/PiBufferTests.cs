using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BBP.Test;

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
            offsetInHexDigits: 10,
            blockLengths: new[] {10, 20, 30});
    }

    [TestMethod]
    public async Task GetPiSegment_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        var piBuffer = CreatePiBuffer();

        var cancellationSource = new CancellationTokenSource();
        // Act
        var piBytes = await piBuffer.GetPiBytesAsync(
                offsetInHexDigits: 10,
                byteCount: 10,
                cancellationToken: cancellationSource.Token)
            .ConfigureAwait(continueOnCapturedContext: false);

        var piBytesTestPrepend = await piBuffer.GetPiBytesAsync(
                offsetInHexDigits: 0,
                byteCount: 20,
                cancellationToken: cancellationSource.Token)
            .ConfigureAwait(continueOnCapturedContext: false);

        var piBytesTestAppend = await piBuffer.GetPiBytesAsync(
                offsetInHexDigits: 0,
                byteCount: 30,
                cancellationToken: cancellationSource.Token)
            .ConfigureAwait(continueOnCapturedContext: false);

        // TODO: it's possible we might be able to screw things up by moving to an odd minimum- idiot proof?
        // starting at 10 - 20 and moving to 11 to 21 or something, or 11 - 21 and moving to 10-22
        var piBytesTestAppendTwice = await piBuffer.GetPiBytesAsync(
                offsetInHexDigits: 0,
                byteCount: 40,
                cancellationToken: cancellationSource.Token)
            .ConfigureAwait(continueOnCapturedContext: false);


        /*
24 3F 6A 88 85 A3 08 D3 13 19 
8A 2E 03 70 73 44 A4 09 38 22
29 9F 31 D0 08 2E FA 98 EC 4E
6C 89 45 28 21 E6 38 D0 13 77
BE 54 66 CF 34 E9 0C 6C C0 AC

piBytes is the second 10 bytes of pi
piBytesTestPrepend is the first 20 bytes of pi
piBytesTestAppend is the first 30 bytes of pi
piBytesTestAppendTwice is the first 40 bytes of pi
         */

        // Assert
        Assert.IsTrue(condition: piBytes.SequenceEqual(second: new byte[] {0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73}));

        Assert.IsTrue(
            condition: piBytesTestPrepend.SequenceEqual(second: new byte[]
            {
                0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73, 0x44, 0xA4, 0x09, 0x38, 0x22,
            }));
        Assert.IsTrue(condition: piBytesTestAppend.SequenceEqual(second: new byte[]
        {
            0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73, 0x44, 0xA4, 0x09, 0x38, 0x22,
            0x29, 0x9F, 0x31, 0xD0, 0x08, 0x2E, 0xFA, 0x98, 0xEC, 0x4E,
        }));
        Assert.IsTrue(condition: piBytesTestAppendTwice.SequenceEqual(second: new byte[]
        {
            0x24, 0x3F, 0x6A, 0x88, 0x85, 0xA3, 0x08, 0xD3, 0x13, 0x19, 0x8A, 0x2E, 0x03, 0x70, 0x73, 0x44, 0xA4, 0x09, 0x38, 0x22,
            0x29, 0x9F, 0x31, 0xD0, 0x08, 0x2E, 0xFA, 0x98, 0xEC, 0x4E, 0x6C, 0x89, 0x45, 0x28, 0x21, 0xE6, 0x38, 0xD0, 0x13, 0x77,
        }));
        mockRepository.VerifyAll();
    }
}
