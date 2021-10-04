using BBP.FasterKVMiner;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace BBP.FasterKVMiner.Test
{
    [TestClass]
    public class TrackerTests
    {
        private MockRepository mockRepository;



        [TestInitialize]
        public void TestInitialize()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);


        }

        private Tracker CreateTracker()
        {
            return new Tracker();
        }

        [TestMethod]
        public void Dispose_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var tracker = this.CreateTracker();

            // Act
            tracker.Dispose();

            // Assert
            Assert.Fail();
            this.mockRepository.VerifyAll();
        }
    }
}
