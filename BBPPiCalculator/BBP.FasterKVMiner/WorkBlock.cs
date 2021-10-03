namespace BBP.FasterKVMiner
{
    public class WorkBlock : IWorkable
    {
        private readonly long StartingOffset;
        private readonly int[] BlockSizes;
        private PiDigit piGenerator;

        public IWorkable AsWorkable() => this;

        public WorkBlock(long startingOffset, int[] blockSizes)
        {
            this.StartingOffset = startingOffset;
            this.BlockSizes = blockSizes;
            this.piGenerator = new PiDigit(nOffset: startingOffset);
        }

        async Task<WorkBlock> IWorkable.Work()
        {
            throw new NotImplementedException();
        }
    }
}
