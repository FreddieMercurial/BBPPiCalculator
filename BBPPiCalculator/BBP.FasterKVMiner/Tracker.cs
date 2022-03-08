namespace BBP.FasterKVMiner;

public class Tracker : IDisposable
{
    private const int MaxThreadCount = 10;

    private readonly int[] _blockSizes;
    private readonly List<long> _failedOffsetsToRetry;
    private readonly FasterKVBBPiMiner _fasterKv;

    private readonly PiBuffer[] _piBytes;
    private readonly CancellationTokenSource _tokenSource;
    private long _piBytesOffset;
    private readonly bool[] _running;
    private readonly Task[] _tasks;
    private readonly WorkBlock?[] _workBlocks;

    public Tracker(string baseDirectory)
    {
        _failedOffsetsToRetry = new List<long>();
        _tokenSource = new CancellationTokenSource();
        _fasterKv = new FasterKVBBPiMiner(baseDirectory: baseDirectory);
        _blockSizes = new[] {128, 256, 512, 1024, 4096, 1048576, 4194304};
        _tasks = new Task[MaxThreadCount];
        _workBlocks = new WorkBlock[MaxThreadCount];
        _running = new bool[MaxThreadCount];
        _piBytes = new PiBuffer[MaxThreadCount];
        for (var threadId = 0; threadId < MaxThreadCount; threadId++)
        {
            _piBytes[threadId] = new PiBuffer(
                offset: _piBytesOffset,
                blockLengths: _blockSizes);
        }
    }

    public void Dispose()
    {
        _tokenSource.Cancel();
        foreach (var task in _tasks)
        {
            task?.Dispose();
        }

        _fasterKv.Dispose();
    }

    public async IAsyncEnumerable<WorkBlock> Run()
    {
        Console.WriteLine(value: $"Starting {MaxThreadCount} threads");

        while (!_tokenSource.IsCancellationRequested)
        {
            for (var threadId = 0; threadId < MaxThreadCount; threadId++)
            {
                if (!_running[threadId])
                {
                    _running[threadId] = true;

                    // get the next starting offset
                    var startingOffset = _piBytesOffset;
                    Console.WriteLine(value: $"Started thread at offset {startingOffset}");

                    var newWorkBlock = new WorkBlock(
                        startingOffset: startingOffset,
                        blockSizes: _blockSizes);

                    // bump the offset by the size of the native ('resolution') block
                    _piBytesOffset += BBPCalculator.NativeChunkSizeInChars;

                    // create a new task
                    var newTask = NewComputationTask(
                        workBlock: newWorkBlock,
                        threadId: threadId,
                        cancellationToken: _tokenSource.Token);

                    _workBlocks[threadId] = newWorkBlock;
                    _tasks[threadId] = newTask;

                    await newTask.ConfigureAwait(continueOnCapturedContext: false);
                }
            }

            var completedThreadId = Task.WaitAny(tasks: _tasks);

            var task = _tasks[completedThreadId];
            // if task is not done, continue
            if (task.Status is not (TaskStatus.Canceled or TaskStatus.Faulted or TaskStatus.RanToCompletion))
            {
                continue;
            }

            if (task.IsCompletedSuccessfully)
            {
                var completedWorkBlock = _workBlocks[completedThreadId]!;
                yield return completedWorkBlock;

                await Console.Out.WriteLineAsync(value: $"+ SUCCESS: @{completedWorkBlock.StartingOffset}")
                    .ConfigureAwait(continueOnCapturedContext: false);
                foreach (var (blockLength, dataHash) in completedWorkBlock.BlockSizeHashes)
                {
                    await Console.Out.WriteLineAsync(value: $"  > {blockLength} byte block with hash {dataHash.ToString()}")
                        .ConfigureAwait(continueOnCapturedContext: false);
                }
            }
            else
            {
                // TODO: actually retry
                _failedOffsetsToRetry.Add(item: _workBlocks[completedThreadId]!.StartingOffset);
                await Console.Error.WriteLineAsync(value: "- FAILED: " + task.Exception!.Message)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }

            task.Dispose();
            _running[completedThreadId] = false;
            _workBlocks[completedThreadId] = null;
        } // end while
    }

    private async Task NewComputationTask(WorkBlock workBlock, int threadId, CancellationToken cancellationToken)
    {
        // ReSharper disable InlineTemporaryVariable
        var threadIdCopy = threadId;
        // ReSharper restore InlineTemporaryVariable

        await workBlock
            .Work(
                workingMemory: _piBytes[threadIdCopy],
                cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        foreach (var blockSize in workBlock.BlockSizes)
        {
            _fasterKv.AddComputation(
                n: workBlock.StartingOffset,
                blockSize: blockSize,
                firstByte: workBlock.FirstByte!.Value,
                sha256: Convert.ToHexString(inArray: workBlock.BlockSizeHashes[key: blockSize].HashBytes.ToArray()));
        }
    }
}
