using System.Collections.Immutable;
using NeuralFabric.Models.Hashes;

namespace BBP.FasterKVMiner;

public class PiBuffer
{
    private const long MaximumMemory = 1024 * 1024 * 1024; // 1GB

    private static readonly Dictionary<long, PiBlock> _piBlocks = new();
    private static readonly Mutex _piBlocksMutex = new();
    private static long _lowestPiBlock = -1;
    private static long _highestPiBlock = -1;

    /// <summary>
    ///     Queues of piBytes sorted by the number of bytes in each block size
    /// </summary>
    private readonly ImmutableDictionary<int, Queue<PiByte>> _workingQueuesByBlockLength;

    /// <summary>
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="blockLengths"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public PiBuffer(long offset, int[] blockLengths)
    {
        if (offset < 0)
        {
            throw new ArgumentException(message: null,
                paramName: nameof(offset));
        }

        if (!blockLengths.Any())
        {
            throw new ArgumentNullException(paramName: nameof(blockLengths));
        }

        var longestBlock = blockLengths.Max();
        if (longestBlock < BBPCalculator.NativeChunkSizeInChars)
        {
            throw new ArgumentException(message: null,
                paramName: nameof(blockLengths));
        }

        var digitQueues = new Dictionary<int, Queue<PiByte>>();
        foreach (var blockLength in blockLengths)
        {
            if (blockLength < BBPCalculator.NativeChunkSizeInChars)
            {
                throw new ArgumentException(
                    message: $"{blockLength} is less than the native chunk size of {BBPCalculator.NativeChunkSizeInChars}",
                    paramName: nameof(blockLengths));
            }

            digitQueues.Add(key: blockLength,
                value: new Queue<PiByte>());
        }

        BlockLengths = blockLengths;
        _workingQueuesByBlockLength = digitQueues.ToImmutableDictionary();
        FirstOffset = offset;
        LastOffset = Offset + longestBlock;
        Offset = offset;
    }

    /// <summary>
    ///     Block lengths being evaluated.
    /// </summary>
    public int[] BlockLengths { get; init; }

    /// <summary>
    ///     First offset evaluated.
    /// </summary>
    public long FirstOffset { get; init; }

    /// <summary>
    ///     Highest offset evaluated.
    /// </summary>
    public long LastOffset { get; init; }

    /// <summary>
    ///     Current offset being evaluated.
    /// </summary>
    public long Offset { get; private set; }

    private long BytesUsed => _piBlocks.Count() * BBPCalculator.NativeChunkSizeInChars;

    public long ClosestOffset(long nOffset)
    {
        return (long)(Math.Floor(d: (double)nOffset / BBPCalculator.NativeChunkSizeInChars) * BBPCalculator.NativeChunkSizeInChars);
    }

    public long GarbageCollect()
    {
        if (BytesUsed <= MaximumMemory)
        {
            return 0;
        }

        var freed = 0;
        while (BytesUsed > MaximumMemory)
        {
            var lowestKey = _piBlocks.Keys.Min();
            _piBlocks.Remove(key: lowestKey);
            freed += BBPCalculator.NativeChunkSizeInChars;
        }

        // update lowest key
        _lowestPiBlock = _piBlocks.Keys.Min();

        Console.WriteLine(value: $"PiBytes GC Freed {freed} bytes");

        return freed;
    }

    public async Task<PiBlock> GetPiBlock(long nOffset, CancellationToken cancellationToken)
    {
        _piBlocksMutex.WaitOne();
        try
        {
            var closestOffset = ClosestOffset(nOffset: nOffset);
            if (_piBlocks.ContainsKey(key: closestOffset))
            {
                return _piBlocks[key: closestOffset];
            }

            var task = new Task<byte[]>(function: () => BBPCalculator.PiBytes(
                n: closestOffset,
                count: BBPCalculator.NativeChunkSizeInChars).ToArray());

            var piBytes = await task
                .WaitAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            if (_lowestPiBlock == -1 || nOffset < _lowestPiBlock)
            {
                _lowestPiBlock = nOffset;
            }

            if (_highestPiBlock == -1 || nOffset > _highestPiBlock)
            {
                _highestPiBlock = nOffset;
            }

            var readonlyBytes = new ReadOnlyMemory<byte>(array: piBytes);
            var block = new PiBlock(
                N: closestOffset,
                Values: readonlyBytes,
                DataHash: new DataHash(dataBytes: readonlyBytes));

            _piBlocks[key: closestOffset] = block;
            GarbageCollect();

            return block;
        }
        finally
        {
            _piBlocksMutex.ReleaseMutex();
        }
    }

    public async Task<byte> GetPiByte(long nOffset, CancellationToken cancellationToken)
    {
        var piBytes = await GetPiBlock(
                nOffset: nOffset,
                cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
        var offsetRemainder = (int)(nOffset % BBPCalculator.NativeChunkSizeInChars);
        return piBytes.Values.ToArray()[offsetRemainder];
    }

    /// <summary>
    ///     Execute a work unit.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async IAsyncEnumerable<PiBlock> Work()
    {
        using var tokenSource = new CancellationTokenSource();
        var cancellationToken = tokenSource.Token;
        var offset = Offset++;
        var task = new Task<byte[]>(function: () => BBPCalculator.PiBytes(
            n: offset,
            count: BlockLengths.Max()).ToArray());

        var piBytes = await task
            .WaitAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        foreach (var piByte in piBytes)
        {
            foreach (var blockLength in BlockLengths)
            {
                _workingQueuesByBlockLength[key: blockLength].Enqueue(
                    item: new PiByte(
                        N: offset,
                        Value: piByte));

                if (_workingQueuesByBlockLength.Count <= blockLength)
                {
                    continue;
                }

                if (blockLength == 1)
                {
                    continue;
                }

                _workingQueuesByBlockLength[key: blockLength].Dequeue();
                var piBytesForLength = _workingQueuesByBlockLength[key: blockLength].ToArray();
                if (piBytesForLength.Any(predicate: p => p.N != offset))
                {
                    throw new Exception(message: "PiBuffer: n _offset mismatch");
                }

                var aggregate = piBytesForLength.Select(selector: p => p.Value).ToArray();
                if (aggregate.Length != blockLength)
                {
                    throw new Exception();
                }

                Console.WriteLine(value: $"Completed {blockLength} block at offset {offset}");

                var readonlyAggregate = new ReadOnlyMemory<byte>(array: aggregate);
                yield return new PiBlock(
                    N: offset,
                    Values: readonlyAggregate,
                    DataHash: new DataHash(dataBytes: readonlyAggregate));
            }
        }
    }
}
