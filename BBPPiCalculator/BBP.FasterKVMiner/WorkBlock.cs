using System.Security.Cryptography;

namespace BBP.FasterKVMiner
{
    public class WorkBlock : IWorkable
    {
        private readonly long StartingOffset;
        private readonly int[] BlockSizes;
        private readonly Dictionary<int, byte[]> BlockSizeHashes;
        private PiDigit piGenerator;

        public IWorkable AsWorkable() => this;

        public WorkBlock(long startingOffset, int[] blockSizes)
        {
            this.StartingOffset = startingOffset;
            this.BlockSizes = blockSizes;
            this.BlockSizeHashes = new Dictionary<int, byte[]>();
            this.piGenerator = new PiDigit(nOffset: startingOffset);
        }

        WorkBlock IWorkable.Work()
        {
            // from the given digit index, compute all sha 256 hashes of all block sizes beginning at that index
            // get the longest block size and then get that many pi bytes from here
            var maxBlockSize = this.BlockSizes.Max();
            var piBytes = this.piGenerator.PiBytes(
                n: this.StartingOffset,
                count: maxBlockSize).Select(c => (byte)c).ToArray();
            foreach (var blockSize in this.BlockSizes)
            {
                using (var sha256 = SHA256.Create())
                {
                    this.BlockSizeHashes[blockSize] = sha256.ComputeHash(
                        buffer: piBytes,
                        offset: 0,
                        count: blockSize);
                }
            }
            return this;
        }
    }
}
