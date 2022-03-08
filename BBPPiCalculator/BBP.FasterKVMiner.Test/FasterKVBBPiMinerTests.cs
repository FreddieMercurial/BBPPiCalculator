using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BBP.FasterKVMiner.Test;

[TestClass]
public class FasterKVBBPiMinerTests
{
    private MockRepository mockRepository;


    [TestInitialize]
    public void TestInitialize()
    {
        mockRepository = new MockRepository(defaultBehavior: MockBehavior.Strict);
    }

    private FasterKVBBPiMiner CreateFasterKVBBPiMiner()
    {
        return new FasterKVBBPiMiner(baseDirectory: Environment.CurrentDirectory);
    }

    [TestMethod]
    public void AddComputation_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        var fasterKVBBPiMiner = CreateFasterKVBBPiMiner();
        long n = 0;
        var blockSize = 0;
        byte firstByte = 0;
        var sha256 = string.Empty;

        // Act
        fasterKVBBPiMiner.AddComputation(
            n: n,
            blockSize: blockSize,
            firstByte: firstByte,
            sha256: sha256);

        // Assert
        Assert.Fail();
        mockRepository.VerifyAll();
    }

    [TestMethod]
    public void Dispose_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        var fasterKVBBPiMiner = CreateFasterKVBBPiMiner();

        // Act
        fasterKVBBPiMiner.Dispose();

        // Assert
        Assert.Fail();
        mockRepository.VerifyAll();
    }
}
