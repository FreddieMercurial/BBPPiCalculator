namespace BBP.FasterKVMiner;

public class Tracker : IDisposable
{
    private const int MaxThreadCount = 10;
    private const int BufferMaxCapacity = 4194304 * MaxThreadCount * 5;

    private readonly int[] _blockSizes;
    private readonly FasterKVBBPiMiner _fasterKv;

    private readonly PiBuffer _piBytes;
    private readonly Thread[]? _threads;
    private readonly long piBytesOffset = 0;

    public Tracker()
    {
        _threads = new Thread[MaxThreadCount];
        Array.Fill(array: _threads, value: null);
        _fasterKv = new FasterKVBBPiMiner();
        _blockSizes = new[] {128, 256, 512, 1024, 4096, 1048576, 4194304};
        _piBytes = new PiBuffer(
            startingCharOffset: piBytesOffset,
            maxByteCapacity: BufferMaxCapacity);
    }

    public void Dispose()
    {
        foreach (var thread in _threads!)
        {
            thread?.Join();
        }

        _fasterKv.Dispose();
    }

    private async Task<WorkBlock> StartWork(long startingOffset)
    {
        var workBlock = new WorkBlock(
            startingOffset: startingOffset,
            blockSizes: _blockSizes);

        return await Task.Run(function: () =>
        {
            return workBlock
                .AsWorkable()
                .Work(workingMemory: _piBytes);
        }).ConfigureAwait(continueOnCapturedContext: false);
    }

    private async Task<WorkBlock> PerformComputation(long startingOffset)
    {
        var resultWorkBlock = await StartWork(startingOffset: startingOffset).ConfigureAwait(continueOnCapturedContext: false);
        foreach (var blockSize in resultWorkBlock.BlockSizes)
        {
            _fasterKv.AddComputation(
                n: resultWorkBlock.StartingOffset,
                blockSize: blockSize,
                firstChar: resultWorkBlock.FirstCharacter,
                sha256: Convert.ToHexString(inArray: resultWorkBlock.BlockSizesHashes[key: blockSize]));
        }

        return resultWorkBlock;
    }
}
