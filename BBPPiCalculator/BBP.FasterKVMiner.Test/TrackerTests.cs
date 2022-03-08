using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BBP.FasterKVMiner.Test;

[TestClass]
public class TrackerTests
{
    private MockRepository mockRepository;


    [TestInitialize]
    public void TestInitialize()
    {
        mockRepository = new MockRepository(defaultBehavior: MockBehavior.Strict);
    }

    private Tracker CreateTracker()
    {
        return new Tracker(baseDirectory: Environment.CurrentDirectory);
    }

    [TestMethod]
    public void Dispose_StateUnderTest_ExpectedBehavior()
    {
        // Arrange
        var tracker = CreateTracker();

        // Act
        tracker.Dispose();

        // Assert
        Assert.Fail();
        mockRepository.VerifyAll();
    }
}
