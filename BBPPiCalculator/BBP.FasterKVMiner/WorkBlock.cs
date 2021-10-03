using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBP.FasterKVMiner
{
    public class WorkBlock
    {
        private readonly long StartingOffset;
        private readonly int[] BlockSizes;

        public WorkBlock(long startingOffset, int[] blockSizes)
        {
            this.StartingOffset = startingOffset;
            this.BlockSizes = blockSizes;
        }
    }
}
