namespace BBP.FasterKVMiner;

public class PiBuffer : IDisposable
{
    private readonly int MaximumByteCapacity;
    private readonly Mutex mutex;
    private long HighestCharOffsetContained;
    private long LowestCharOffsetContained;
    private Dictionary<int, Queue<PiByte>> PiDigitQueuesByLength;
    private byte[] WorkingMemory;

    public PiBuffer(long startingCharOffset, int maxByteCapacity)
    {
        if (startingCharOffset < 0)
        {
            throw new ArgumentException(message: null, paramName: nameof(startingCharOffset));
        }

        if (maxByteCapacity < BBPCalculator.NativeChunkSizeInChars)
        {
            throw new ArgumentException(message: null, paramName: nameof(maxByteCapacity));
        }

        MaximumByteCapacity = maxByteCapacity;
        LowestCharOffsetContained = startingCharOffset;
        HighestCharOffsetContained = startingCharOffset + BBPCalculator.NativeChunkSizeInChars;
        var memorySize = (int)(HighestCharOffsetContained - LowestCharOffsetContained);
        WorkingMemory = BBPCalculator.PiBytes(
            n: LowestCharOffsetContained,
            count: memorySize).ToArray();
        PiDigitQueuesByLength = new Dictionary<int, Queue<PiByte>>();
        mutex = new Mutex(initiallyOwned: false);
    }

    private (long, long) CurrentRangeUnsafe
        => (LowestCharOffsetContained, HighestCharOffsetContained);

    public (long, long) CurrentRange
    {
        get
        {
            mutex.WaitOne();
            var range = CurrentRangeUnsafe;
            mutex.ReleaseMutex();
            return range;
        }
    }

    public void Dispose()
    {
        mutex.WaitOne();
        mutex.Dispose();
    }

    private void Ensure(long minimum, long maximum, bool useMutex = true)
    {
        if (minimum < 0 || minimum > maximum)
        {
            throw new ArgumentException(message: null, paramName: nameof(minimum));
        }

        if (maximum < minimum)
        {
            throw new ArgumentException(message: null, paramName: nameof(maximum));
        }

        if ((maximum - minimum) % 2 != 0)
        {
            throw new ArgumentException(message: "spread must be multiple of 2 characters");
        }

        if (
            minimum >= LowestCharOffsetContained &&
            minimum <= HighestCharOffsetContained &&
            maximum >= LowestCharOffsetContained &&
            maximum <= HighestCharOffsetContained
        )
        {
            return;
        }

        try
        {
            if (useMutex)
            {
                mutex.WaitOne();
            }

            GarbageCollect(
                requestedMinimum: minimum,
                requestedMaximum: maximum);

            if (minimum < LowestCharOffsetContained)
            {
                // add bytes on the left
                var charsNeeded = (int)(LowestCharOffsetContained - minimum);
                var bytesNeeded = charsNeeded / 2;
                var leftMemory = BBPCalculator.PiBytes(
                    n: minimum,
                    count: bytesNeeded).ToArray();
                Array.Resize(array: ref leftMemory, newSize: WorkingMemory.Length + bytesNeeded);
                Array.Copy(
                    sourceArray: WorkingMemory,
                    sourceIndex: 0,
                    destinationArray: leftMemory,
                    destinationIndex: bytesNeeded,
                    length: WorkingMemory.Length);
                WorkingMemory = leftMemory;
                LowestCharOffsetContained = minimum;
            }

            if (maximum > HighestCharOffsetContained)
            {
                var charsNeeded = (int)(maximum - HighestCharOffsetContained);
                var bytesNeeded = charsNeeded / 2;
                var rightMemory = BBPCalculator.PiBytes(
                    n: HighestCharOffsetContained + 1,
                    count: bytesNeeded).ToArray();
                var oldLength = WorkingMemory.Length;
                Array.Resize(array: ref WorkingMemory, newSize: oldLength + bytesNeeded);
                Array.Copy(
                    sourceArray: rightMemory,
                    sourceIndex: 0,
                    destinationArray: WorkingMemory,
                    destinationIndex: oldLength,
                    length: bytesNeeded);
                HighestCharOffsetContained = maximum;
            }
        }
        finally
        {
            if (useMutex)
            {
                mutex.ReleaseMutex();
            }
        }
    }

    public byte[] GetPiSegment(long minimum, long maximum)
    {
        if ((maximum - minimum) % 2 != 0)
        {
            throw new ArgumentException(message: "spread must be multiple of 2 characters");
        }

        try
        {
            mutex.WaitOne();

            var charsNeeded = maximum - minimum;
            var bytesNeeded = (int)(charsNeeded / 2);
            var returnArray = new byte[bytesNeeded];

            Ensure(
                minimum: minimum,
                maximum: maximum,
                useMutex: false);

            var distanceFromZero = (LowestCharOffsetContained - minimum) / 2;
            Array.Copy(
                sourceArray: WorkingMemory,
                sourceIndex: distanceFromZero,
                destinationArray: returnArray,
                destinationIndex: 0,
                length: returnArray.Length);

            return returnArray;
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private void GarbageCollect(long requestedMinimum, long requestedMaximum)
    {
        var charsRequested = requestedMaximum - requestedMinimum;
        if (charsRequested / 2 > MaximumByteCapacity)
        {
            PruneLeft(newMinimum: requestedMaximum);
        }
    }

    private void PruneLeft(long newMinimum)
    {
        if (newMinimum < LowestCharOffsetContained || newMinimum > HighestCharOffsetContained)
        {
            throw new ArgumentException(message: null, paramName: nameof(newMinimum));
        }

        var charsToRemove = newMinimum - LowestCharOffsetContained;
        var bytesToRemove = (int)(charsToRemove / 2);
        var newSize = WorkingMemory.Length - bytesToRemove;
        if (bytesToRemove <= 0)
        {
            return;
        }

        var distanceFromZero = (LowestCharOffsetContained - newMinimum) / 2;
        var newBytes = new byte[newSize];
        Array.Copy(
            sourceArray: WorkingMemory,
            sourceIndex: distanceFromZero,
            destinationArray: newBytes,
            destinationIndex: 0,
            length: newBytes.Length);
        WorkingMemory = newBytes;
    }
}
