namespace BBP.FasterKVMiner;

public class Tracker : IDisposable
{
    private const int MaxThreadCount = 10;

    private readonly int[] _blockSizes;
    private readonly FasterKVBBPiMiner _fasterKv;

    private readonly PiBuffer _piBytes;
    private readonly Thread[]? _threads;
    private readonly long piBytesOffset = 0;

    public Tracker(string baseDirectory)
    {
        _threads = new Thread[MaxThreadCount];
        Array.Fill(array: _threads,
            value: null);
        _fasterKv = new FasterKVBBPiMiner(baseDirectory: baseDirectory);
        _blockSizes = new[] {128, 256, 512, 1024, 4096, 1048576, 4194304};
        _piBytes = new PiBuffer(
            offset: piBytesOffset,
            blockLengths: _blockSizes);
    }

    public void Dispose()
    {
        foreach (var thread in _threads!)
        {
            thread?.Join();
        }

        _fasterKv.Dispose();
    }


    private async Task<WorkBlock> WorkAndLogComputation(long startingOffset)
    {
        var workBlock = new WorkBlock(
            startingOffset: startingOffset,
            blockSizes: _blockSizes);

        await workBlock
            .Work(workingMemory: _piBytes)
            .ConfigureAwait(continueOnCapturedContext: false);

        foreach (var blockSize in workBlock.BlockSizes)
        {
            _fasterKv.AddComputation(
                n: workBlock.StartingOffset,
                blockSize: blockSize,
                firstByte: workBlock.FirstByte!.Value,
                sha256: Convert.ToHexString(inArray: workBlock.BlockSizesHashes[key: blockSize]));
        }

        return workBlock;
    }
}
