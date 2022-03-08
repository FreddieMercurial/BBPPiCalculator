using System.Collections.Immutable;
using NeuralFabric.Models.Hashes;

namespace BBP;

public class PiBuffer
{
    private const long MaximumMemory = 1024 * 1024 * 1024; // 1GB

    private static readonly Dictionary<long, PiBlock> _piBlocks = new();
    private static long _lowestPiBlock = -1;
    private static long _highestPiBlock = -1;

    /// <summary>
    ///     Queues of piBytes sorted by the number of bytes in each block size
    /// </summary>
    private readonly ImmutableDictionary<int, Queue<PiByte>> _workingQueuesByBlockLength;

    /// <summary>
    /// </summary>
    /// <param name="offsetInHexDigits"></param>
    /// <param name="blockLengths"></param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public PiBuffer(long offsetInHexDigits, int[] blockLengths)
    {
        if (offsetInHexDigits < 0)
        {
            throw new ArgumentException(message: null,
                paramName: nameof(offsetInHexDigits));
        }

        if (!blockLengths.Any())
        {
            throw new ArgumentNullException(paramName: nameof(blockLengths));
        }

        var longestBlock = blockLengths.Max();
        if (longestBlock < BBPCalculator.NativeChunkSizeInBytes)
        {
            throw new ArgumentException(message: null,
                paramName: nameof(blockLengths));
        }

        var digitQueues = new Dictionary<int, Queue<PiByte>>();
        foreach (var blockLength in blockLengths)
        {
            if (blockLength < BBPCalculator.NativeChunkSizeInBytes)
            {
                throw new ArgumentException(
                    message: $"{blockLength} is less than the native chunk size of {BBPCalculator.NativeChunkSizeInBytes}",
                    paramName: nameof(blockLengths));
            }

            digitQueues.Add(key: blockLength,
                value: new Queue<PiByte>());
        }

        BlockLengths = blockLengths;
        _workingQueuesByBlockLength = digitQueues.ToImmutableDictionary();
        FirstOffsetInHexDigits = offsetInHexDigits;
        LastOffsetInHexDigits = OffsetInHexDigits + longestBlock;
        OffsetInHexDigits = offsetInHexDigits;
    }

    /// <summary>
    ///     Block lengths being evaluated.
    /// </summary>
    public int[] BlockLengths { get; init; }

    /// <summary>
    ///     First offset evaluated.
    /// </summary>
    public long FirstOffsetInHexDigits { get; init; }

    /// <summary>
    ///     Highest offset evaluated.
    /// </summary>
    public long LastOffsetInHexDigits { get; init; }

    /// <summary>
    ///     Current offset being evaluated.
    /// </summary>
    public long OffsetInHexDigits { get; }

    private long BytesUsed => _piBlocks.Count() * BBPCalculator.NativeChunkSizeInBytes;

    public long ClosestOffsetInHexDigitChars(long nOffset)
    {
        return (long)(Math.Floor(d: (double)nOffset / BBPCalculator.NativeChunkSizeInHexDigitChars) *
                      BBPCalculator.NativeChunkSizeInHexDigitChars);
    }

    public int OffsetRemainderInHexDigitChars(long nOffset)
    {
        return (int)(nOffset % BBPCalculator.NativeChunkSizeInHexDigitChars);
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
            freed += BBPCalculator.NativeChunkSizeInBytes;
        }

        // update lowest key
        _lowestPiBlock = _piBlocks.Keys.Min();

        Console.WriteLine(value: $"PiBytes GC Freed {freed} bytes");

        return freed;
    }

    public async Task<PiBlock> GetFixedOffsetPiBlockAsync(long offsetInHexDigits, CancellationToken cancellationToken)
    {
        var closestOffset = ClosestOffsetInHexDigitChars(nOffset: offsetInHexDigits);
        if (_piBlocks.ContainsKey(key: closestOffset))
        {
            return _piBlocks[key: closestOffset];
        }

        var piBytes = new List<byte>();
        await foreach (var piByte in BBPCalculator.PiBytesAsync(
                               offsetInHexDigitChars: closestOffset,
                               byteCount: BBPCalculator.NativeChunkSizeInBytes)
                           .WithCancellation(cancellationToken: cancellationToken)
                           .ConfigureAwait(continueOnCapturedContext: false))
        {
            piBytes.Add(item: piByte);
        }

        if (_lowestPiBlock == -1 || offsetInHexDigits < _lowestPiBlock)
        {
            _lowestPiBlock = offsetInHexDigits;
        }

        if (_highestPiBlock == -1 || offsetInHexDigits > _highestPiBlock)
        {
            _highestPiBlock = offsetInHexDigits;
        }

        var readonlyBytes = new ReadOnlyMemory<byte>(array: piBytes.ToArray());
        var block = new PiBlock(
            N: closestOffset,
            Values: readonlyBytes,
            DataHash: new DataHash(dataBytes: readonlyBytes));

        _piBlocks[key: closestOffset] = block;
        GarbageCollect();

        return block;
    }

    public async Task<byte> GetPiByteAsync(long offsetInHexDigits, CancellationToken cancellationToken)
    {
        var piBytes = await GetFixedOffsetPiBlockAsync(
                offsetInHexDigits: offsetInHexDigits,
                cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);
        return piBytes.Values.ToArray()[OffsetRemainderInHexDigitChars(nOffset: offsetInHexDigits)];
    }

    public async Task<IEnumerable<byte>> GetPiBytesAsync(long offsetInHexDigits, int byteCount, CancellationToken cancellationToken)
    {
        var closestOffsetInHexDigitChars = ClosestOffsetInHexDigitChars(nOffset: offsetInHexDigits);
        var fixedArrayOffset = OffsetRemainderInHexDigitChars(nOffset: offsetInHexDigits);

        var piBytes = new List<byte>(capacity: byteCount);
        var remainingBytes = byteCount;
        while (remainingBytes > 0)
        {
            var fixedOffsetBlock = await GetFixedOffsetPiBlockAsync(
                    offsetInHexDigits: closestOffsetInHexDigitChars,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            var fixedOffsetBytes = fixedOffsetBlock.Values.ToArray();
            closestOffsetInHexDigitChars += BBPCalculator.NativeChunkSizeInHexDigitChars;

            var lengthToAdd = Math.Min(
                val1: BBPCalculator.NativeChunkSizeInBytes,
                val2: remainingBytes);

            var bytesToAdd = fixedOffsetBytes
                .Skip(count: fixedArrayOffset)
                .Take(count: lengthToAdd)
                .ToArray();

            piBytes.AddRange(collection: bytesToAdd);
            remainingBytes -= bytesToAdd.Length;

            // reset the array offset
            fixedArrayOffset = 0;
        }

        return piBytes;
    }

    /// <summary>
    ///     Find the sub-sequences of a pi block.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public IEnumerable<PiBlock> EnumerateSubBlocks(PiBlock fixedIntervalPiBlock)
    {
        var offset = fixedIntervalPiBlock.N;
        foreach (var piByte in fixedIntervalPiBlock.Values.ToArray())
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

            offset++;
        }
    }
}
