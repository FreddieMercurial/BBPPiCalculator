using System.Collections.Immutable;
using System.Security.Cryptography;

namespace BBP.FasterKVMiner;

public class WorkBlock : IWorkable
{
    private readonly Dictionary<int, byte[]> BlockSizeHashBuffer;
    public readonly int[] BlockSizes;
    public readonly long StartingOffset;
    private char firstCharacter;

    public WorkBlock(long startingOffset, int[] blockSizes)
    {
        StartingOffset = startingOffset;
        BlockSizes = blockSizes;
        BlockSizeHashBuffer = new Dictionary<int, byte[]>();
    }

    public ImmutableDictionary<int, byte[]> BlockSizesHashes
        => Completed ? BlockSizeHashBuffer.ToImmutableDictionary() : throw new Exception(message: "result not complete");

    public bool Completed { get; private set; }

    public char FirstCharacter => Completed ? firstCharacter : throw new Exception(message: "result not complete");

    WorkBlock IWorkable.Work(PiBuffer workingMemory)
    {
        // from the given digit index, compute all sha 256 hashes of all block sizes beginning at that index
        // get the longest block size and then get that many pi bytes from here
        var maxBlockSize = BlockSizes.Max();
        var activeSegment = workingMemory.GetPiSegment(
            minimum: StartingOffset,
            maximum: StartingOffset + maxBlockSize);

        firstCharacter = Convert.ToString(
            value: (byte)((activeSegment[0] >> 4) & 0x0F),
            toBase: 16)[index: 0];

        foreach (var blockSize in BlockSizes)
        {
            using (var sha256 = SHA256.Create())
            {
                BlockSizeHashBuffer[key: blockSize] = sha256.ComputeHash(
                    buffer: activeSegment,
                    offset: 0,
                    count: blockSize);
            }
        }

        Completed = true;
        return this;
    }

    public IWorkable AsWorkable()
    {
        return this;
    }
}
