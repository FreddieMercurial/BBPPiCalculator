using System.Collections.Immutable;
using System.Security.Cryptography;

namespace BBP.FasterKVMiner;

public class WorkBlock : IWorkable
{
    private readonly Dictionary<int, byte[]> _blockSizeHashBuffer;
    public readonly int[] BlockSizes;
    public readonly long StartingOffset;
    public byte? FirstByte;

    public WorkBlock(long startingOffset, int[] blockSizes)
    {
        StartingOffset = startingOffset;
        BlockSizes = blockSizes;
        _blockSizeHashBuffer = new Dictionary<int, byte[]>();
    }

    public ImmutableDictionary<int, byte[]> BlockSizesHashes
        => Completed ? _blockSizeHashBuffer.ToImmutableDictionary() : throw new Exception(message: "result not complete");

    public bool Completed { get; private set; }

    public async Task Work(PiBuffer workingMemory)
    {
        using var sha256 = SHA256.Create();
        await foreach (var (nOffset, blockEnumerable) in workingMemory.Work())
        {
            var blockArray = blockEnumerable.ToArray();
            var blockSize = blockArray.Length;
            var blockHash = sha256.ComputeHash(
                buffer: blockArray,
                offset: 0,
                count: blockSize);
            _blockSizeHashBuffer[key: blockSize] = blockHash;
            FirstByte ??= blockArray[0];
            Console.WriteLine(value: $"{blockSize}@{nOffset}: {blockHash:X}");
        }

        Completed = true;
    }
}
