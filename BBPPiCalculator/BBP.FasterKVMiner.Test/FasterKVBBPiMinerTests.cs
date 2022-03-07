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
        return new FasterKVBBPiMiner();
    }

    [TestMethod]
    public void AddComputation_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        var fasterKVBBPiMiner = CreateFasterKVBBPiMiner();
        long n = 0;
        var blockSize = 0;
        var firstChar = default(Char);
        string sha256 = null;

        // Act
        fasterKVBBPiMiner.AddComputation(
            n: n,
            blockSize: blockSize,
            firstChar: firstChar,
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
