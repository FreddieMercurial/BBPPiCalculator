using System.Collections.Immutable;
using System.Security.Cryptography;

namespace BBP.FasterKVMiner
{
    public class WorkBlock : IWorkable
    {
        public readonly long StartingOffset;
        public readonly int[] BlockSizes;
        private readonly Dictionary<int, byte[]> BlockSizeHashBuffer;
        public ImmutableDictionary<int, byte[]> BlockSizesHashes
            => this.completed ? BlockSizeHashBuffer.ToImmutableDictionary() : throw new Exception("result not complete");
        public IWorkable AsWorkable() => this;
        private bool completed = false;
        private char firstCharacter;
        public bool Completed => this.completed;
        public char FirstCharacter => this.completed ? this.firstCharacter : throw new Exception("result not complete");
        public WorkBlock(long startingOffset, int[] blockSizes)
        {
            this.StartingOffset = startingOffset;
            this.BlockSizes = blockSizes;
            this.BlockSizeHashBuffer = new Dictionary<int, byte[]>();
        }

        WorkBlock IWorkable.Work(PiByteBuffer workingMemory)
        {
            // from the given digit index, compute all sha 256 hashes of all block sizes beginning at that index
            // get the longest block size and then get that many pi bytes from here
            var maxBlockSize = this.BlockSizes.Max();
            var activeSegment = workingMemory.GetPiSegment(
                minimum: this.StartingOffset,
                maximum: this.StartingOffset + maxBlockSize);

            this.firstCharacter = Convert.ToString(
                value: (byte)((activeSegment[0] >> 4) & 0x0F),
                toBase: 16)[0];

            foreach (var blockSize in this.BlockSizes)
            {
                using (var sha256 = SHA256.Create())
                {
                    this.BlockSizeHashBuffer[blockSize] = sha256.ComputeHash(
                        buffer: activeSegment,
                        offset: 0,
                        count: blockSize);
                }
            }
            this.completed = true;
            return this;
        }
    }
}
