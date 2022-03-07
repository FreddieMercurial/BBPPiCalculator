using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BBP.FasterKVMiner.Test;

[TestClass]
public class WorkBlockTests
{
    private MockRepository mockRepository;


    [TestInitialize]
    public void TestInitialize()
    {
        mockRepository = new MockRepository(defaultBehavior: MockBehavior.Strict);
    }

    private WorkBlock CreateWorkBlock()
    {
        return new WorkBlock(
            startingOffset: 0,
            blockSizes: new[] {10, 20, 21, 40});
    }

    [TestMethod]
    public void AsWorkable_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        var workBlock = CreateWorkBlock();

        // Act
        var result = workBlock.AsWorkable();

        // Assert
        Assert.Fail();
        mockRepository.VerifyAll();
    }
}
