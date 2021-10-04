using BBP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace BBP.FasterKVMiner.Test
{
    [TestClass]
    public class FasterKVBBPiMinerTests
    {
        private MockRepository mockRepository;



        [TestInitialize]
        public void TestInitialize()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);


        }

        private FasterKVBBPiMiner CreateFasterKVBBPiMiner()
        {
            return new FasterKVBBPiMiner();
        }

        [TestMethod]
        public void AddComputation_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var fasterKVBBPiMiner = this.CreateFasterKVBBPiMiner();
            long n = 0;
            int blockSize = 0;
            char firstChar = default(global::System.Char);
            string sha256 = null;

            // Act
            fasterKVBBPiMiner.AddComputation(
                n,
                blockSize,
                firstChar,
                sha256);

            // Assert
            Assert.Fail();
            this.mockRepository.VerifyAll();
        }

        [TestMethod]
        public void Dispose_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            var fasterKVBBPiMiner = this.CreateFasterKVBBPiMiner();

            // Act
            fasterKVBBPiMiner.Dispose();

            // Assert
            Assert.Fail();
            this.mockRepository.VerifyAll();
        }
    }
}
