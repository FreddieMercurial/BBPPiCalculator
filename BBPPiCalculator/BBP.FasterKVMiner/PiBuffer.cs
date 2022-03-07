using System.Collections.Immutable;

namespace BBP.FasterKVMiner;

public class PiBuffer
{
    /// <summary>
    ///     Block lengths being evaluated.
    /// </summary>
    private readonly int[] _blockLengths;

    /// <summary>
    ///     Queues of piBytes sorted by the number of bytes in each block size
    /// </summary>
    private readonly ImmutableDictionary<int, Queue<PiByte>> _digitQuesByBlockLength;

    /// <summary>
    ///     First offset evaluated.
    /// </summary>
    private readonly long _firstOffset;

    /// <summary>
    ///     Last offset to be evaluated.
    /// </summary>
    private readonly long _lastOffset;

    /// <summary>
    ///     Current offset being evaluated.
    /// </summary>
    private long _offset;

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
            digitQueues[key: blockLength] = new Queue<PiByte>();
        }

        _firstOffset = offset;
        _lastOffset = _offset + longestBlock;
        _offset = offset;
        _blockLengths = blockLengths;
        _digitQuesByBlockLength = digitQueues.ToImmutableDictionary();
    }

    /// <summary>
    ///     First offset evaluated
    /// </summary>
    public long FirstOffset => _firstOffset;

    /// <summary>
    ///     Last offset to be evaluated
    /// </summary>
    public long LastOffset => _lastOffset;

    /// <summary>
    ///     Execute a work unit.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async IAsyncEnumerable<PiBlock> Work()
    {
        using var tokenSource = new CancellationTokenSource();
        var cancellationToken = tokenSource.Token;
        var offset = _offset++;
        var task = new Task<byte[]>(function: () => BBPCalculator.PiBytes(
            n: offset,
            count: _blockLengths.Max()).ToArray());

        var piBytes = await task
            .WaitAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(continueOnCapturedContext: false);

        foreach (var piByte in piBytes)
        {
            foreach (var blockLength in _blockLengths)
            {
                _digitQuesByBlockLength[key: blockLength].Enqueue(
                    item: new PiByte(
                        N: offset,
                        Value: piByte));

                if (_digitQuesByBlockLength.Count <= blockLength)
                {
                    continue;
                }

                _digitQuesByBlockLength[key: blockLength].Dequeue();
                var piBytesForLength = _digitQuesByBlockLength[key: blockLength].ToArray();
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

                yield return new PiBlock(
                    N: offset,
                    Values: aggregate);
            }
        }
    }
}
