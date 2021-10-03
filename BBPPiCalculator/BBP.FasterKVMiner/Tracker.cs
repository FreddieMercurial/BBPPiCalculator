namespace BBP.FasterKVMiner
{
    public class Tracker : IDisposable
    {
        private FasterKVBBPiMiner fasterKV;

        private readonly int[] BlockSizes = new int[]
        {
            128,
            256,
            512,
            1024,
            4096,
            1048576,
            4194304,
        };

        public Tracker()
        {
            this.fasterKV = new FasterKVBBPiMiner();
        }

        public async Task<WorkBlock> StartWork(long startingOffset)
        {
            var workBlock = new WorkBlock(
                startingOffset: startingOffset,
                blockSizes: this.BlockSizes);

            return await Task<WorkBlock>.Run(() =>
            {
                return workBlock
                    .AsWorkable()
                    .Work();
            }).ConfigureAwait(false);
        }

        public void Dispose()
        {
            this.fasterKV.Dispose();
        }
    }
}
