using System.Collections.Immutable;
using System.Security.Cryptography;
using NeuralFabric.Models.Hashes;

namespace BBP.FasterKVMiner;

public class WorkBlock : IWorkable
{
    private readonly Dictionary<int, DataHash> _blockSizeHashBuffer;
    public readonly int[] BlockSizes;
    public readonly long StartingOffset;
    public byte? FirstByte;

    public WorkBlock(long startingOffset, int[] blockSizes)
    {
        StartingOffset = startingOffset;
        BlockSizes = blockSizes;
        _blockSizeHashBuffer = new Dictionary<int, DataHash>();
    }

    public ImmutableDictionary<int, DataHash> BlockSizeHashes
        => Completed ? _blockSizeHashBuffer.ToImmutableDictionary() : throw new Exception(message: "result not complete");

    public bool Completed { get; private set; }

    public async Task Work(PiBuffer workingMemory, CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();

        var fixedIntervalPiBlock = await workingMemory.GetFixedOffsetPiBlockAsync(
                offsetInHexDigits: StartingOffset,
                cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        foreach (var (nOffset, blockEnumerable, blockHash) in workingMemory
                     .EnumerateSubBlocks(fixedIntervalPiBlock: fixedIntervalPiBlock))
        {
            var blockArray = blockEnumerable.ToArray();
            var blockSize = blockArray.Length;
            _blockSizeHashBuffer[key: blockSize] = blockHash;
            FirstByte ??= blockArray[0];
            Console.WriteLine(value: $"{blockSize}@{nOffset}: {blockHash:X}");
        }

        Completed = true;
    }
}
