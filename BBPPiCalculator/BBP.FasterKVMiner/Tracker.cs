namespace BBP.FasterKVMiner;

public class Tracker : IDisposable
{
    private const int MaxThreadCount = 10;

    private readonly int[] _blockSizes;
    private readonly FasterKVBBPiMiner _fasterKv;

    private readonly PiBuffer[] _piBytes;
    private readonly Mutex _threadMutex = new();
    private readonly Thread[] _threads;
    private long _piBytesOffset;

    public Tracker(string baseDirectory)
    {
        _fasterKv = new FasterKVBBPiMiner(baseDirectory: baseDirectory);
        _blockSizes = new[] {128, 256, 512, 1024, 4096, 1048576, 4194304};
        _threads = new Thread[MaxThreadCount];
        _piBytes = new PiBuffer[MaxThreadCount];
        for (var threadId = 0; threadId < MaxThreadCount; threadId++)
        {
            var id = threadId;

            _piBytes[id] = new PiBuffer(
                offset: _piBytesOffset,
                blockLengths: _blockSizes);
            _threads[id] = new Thread(start: () => ThreadStart(threadId: id));
        }
    }

    private async void ThreadStart(int threadId)
    {
        _threadMutex.WaitOne();
        var startingOffset = _piBytesOffset;
        _piBytesOffset += BBPCalculator.NativeChunkSizeInChars;
        _threadMutex.ReleaseMutex();

        var completedBlock = await WorkAndLogComputation(
                startingOffset: startingOffset,
                threadId: threadId)
            .ConfigureAwait(continueOnCapturedContext: false);

        Console.WriteLine(value: $"Completed block @{completedBlock.StartingOffset}");
    }

    public void Dispose()
    {
        foreach (var thread in _threads!)
        {
            thread?.Join();
        }

        _fasterKv.Dispose();
    }

    public async Task Run(int timeout = -1)
    {
        var i = 0;
        var tasks = new Task[_threads.Length];
        foreach (var thread in _threads)
        {
            tasks[i++] = Task.Run(() =>
            {
                thread.Start();
            });
        }
        Console.WriteLine(value: $"{i} threads running");

        if (timeout == -1)
            Task.WaitAll(tasks);
        else
            Task.WaitAll(tasks,
                timeout: TimeSpan.FromSeconds(timeout));
    }


    private async Task<WorkBlock> WorkAndLogComputation(long startingOffset, int threadId)
    {
        var workBlock = new WorkBlock(
            startingOffset: startingOffset,
            blockSizes: _blockSizes);

        await workBlock
            .Work(workingMemory: _piBytes[threadId])
            .ConfigureAwait(continueOnCapturedContext: false);

        foreach (var blockSize in workBlock.BlockSizes)
        {
            _fasterKv.AddComputation(
                n: workBlock.StartingOffset,
                blockSize: blockSize,
                firstByte: workBlock.FirstByte!.Value,
                sha256: Convert.ToHexString(inArray: workBlock.BlockSizesHashes[key: blockSize].HashBytes.ToArray()));
        }

        return workBlock;
    }
}
