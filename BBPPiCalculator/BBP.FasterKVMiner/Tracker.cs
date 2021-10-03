namespace BBP.FasterKVMiner
{
    public class Tracker : IDisposable
    {
        private FasterKVBBPiMiner fasterKV;

        /// <summary>
        /// Example
        /// </summary>
        private readonly int[] BlockSizes = new int[]
        {
            10,
            100,
            1000,
            10000
        };

        public Tracker()
        {
            this.fasterKV = new FasterKVBBPiMiner();
        }

        public async Task<WorkBlock> StartWork(long startingOffset)
        {
            PiDigit pd = new PiDigit(startingOffset);
            var workBlock = new WorkBlock(
                startingOffset: startingOffset,
                blockSizes: this.BlockSizes);
            return workBlock;
        }

        public void Dispose()
        {
            this.fasterKV.Dispose();
        }
    }
}
