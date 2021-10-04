namespace BBP.FasterKVMiner
{
    public class PiBuffer : IDisposable
    {
        private PiDigit piGenerator;
        private long LowestCharOffsetContained;
        private long HighestCharOffsetContained;
        private byte[] WorkingMemory;
        private readonly int MaximumByteCapacity;
        private Mutex mutex;
        private const int NativeChunkSizeInChars = 10;

        public PiBuffer(long startingCharOffset, int maxByteCapacity)
        {
            if (startingCharOffset < 0)
            {
                throw new ArgumentException(nameof(startingCharOffset));
            }
            else if (maxByteCapacity < NativeChunkSizeInChars)
            {
                throw new ArgumentException(nameof(maxByteCapacity));
            }

            this.MaximumByteCapacity = maxByteCapacity;
            this.piGenerator = new PiDigit(nOffset: startingCharOffset);
            this.LowestCharOffsetContained = startingCharOffset;
            this.HighestCharOffsetContained = startingCharOffset + NativeChunkSizeInChars;
            var memorySize = (int)(HighestCharOffsetContained - LowestCharOffsetContained);
            this.WorkingMemory = piGenerator.PiBytes(
                n: LowestCharOffsetContained,
                count: memorySize).ToArray();
            this.mutex = new Mutex(false);
        }

        private (long, long) CurrentRangeUnsafe
            => (this.LowestCharOffsetContained, this.HighestCharOffsetContained);

        public (long, long) CurrentRange
        {
            get
            {
                this.mutex.WaitOne();
                var range = this.CurrentRangeUnsafe;
                this.mutex.ReleaseMutex();
                return range;
            }
        }

        private void Ensure(long minimum, long maximum, bool useMutex = true)
        {
            if ((minimum < 0) || (minimum > maximum))
            {
                throw new ArgumentException(nameof(minimum));
            }
            else if ((maximum < 0) || (maximum < minimum))
            {
                throw new ArgumentException(nameof(maximum));
            }
            else if ((maximum - minimum) % 2 != 0)
            {
                throw new ArgumentException("spread must be multiple of 2 characters");
            }
            else if (
                (minimum >= LowestCharOffsetContained) &&
                (minimum <= HighestCharOffsetContained) &&
                (maximum >= LowestCharOffsetContained) &&
                (maximum <= HighestCharOffsetContained)
            )
            {
                return;
            }

            try
            {
                if (useMutex)
                {
                    this.mutex.WaitOne();
                }

                this.GarbageCollect(
                    requestedMinimum: minimum,
                    requestedMaximum: maximum);

                if (minimum < LowestCharOffsetContained)
                {

                    // add bytes on the left
                    var charsNeeded = (int)(LowestCharOffsetContained - minimum);
                    var bytesNeeded = (int)(charsNeeded / 2);
                    var leftMemory = this.piGenerator.PiBytes(
                        n: minimum,
                        count: bytesNeeded).ToArray();
                    Array.Resize(ref leftMemory, WorkingMemory.Length + bytesNeeded);
                    Array.Copy(
                        sourceArray: WorkingMemory,
                        sourceIndex: 0,
                        destinationArray: leftMemory,
                        destinationIndex: bytesNeeded,
                        length: WorkingMemory.Length);
                    this.WorkingMemory = leftMemory;
                    this.LowestCharOffsetContained = minimum;
                }

                if (maximum > HighestCharOffsetContained)
                {
                    var charsNeeded = (int)(maximum - HighestCharOffsetContained);
                    var bytesNeeded = (int)(charsNeeded / 2);
                    var rightMemory = this.piGenerator.PiBytes(
                        n: this.HighestCharOffsetContained + 1,
                        count: bytesNeeded).ToArray();
                    var oldLength = WorkingMemory.Length;
                    Array.Resize(ref WorkingMemory, oldLength + bytesNeeded);
                    Array.Copy(
                        sourceArray: rightMemory,
                        sourceIndex: 0,
                        destinationArray: WorkingMemory,
                        destinationIndex: oldLength,
                        length: bytesNeeded);
                    this.HighestCharOffsetContained = maximum;
                }
            }
            finally
            {
                if (useMutex)
                {
                    this.mutex.ReleaseMutex();
                }
            }
        }

        public byte[] GetPiSegment(long minimum, long maximum)
        {
            if ((maximum - minimum) % 2 != 0)
            {
                throw new ArgumentException("spread must be multiple of 2 characters");
            }
            try
            {
                this.mutex.WaitOne();

                var charsNeeded = maximum - minimum;
                var bytesNeeded = (int)(charsNeeded / 2);
                var returnArray = new byte[bytesNeeded];

                this.Ensure(
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
                this.mutex.ReleaseMutex();
            }
        }

        private void GarbageCollect(long requestedMinimum, long requestedMaximum)
        {
            var charsRequested = requestedMaximum - requestedMinimum;
            if ((charsRequested / 2) > MaximumByteCapacity)
            {
                this.PruneLeft(newMinimum: requestedMaximum);
            }
        }

        private void PruneLeft(long newMinimum)
        {
            if ((newMinimum < LowestCharOffsetContained) || (newMinimum > HighestCharOffsetContained))
            {
                throw new ArgumentException(nameof(newMinimum));
            }
            var charsToRemove = newMinimum - LowestCharOffsetContained;
            var bytesToRemove = (int)(charsToRemove / 2);
            var newSize = this.WorkingMemory.Length - bytesToRemove;
            if (bytesToRemove > 0)
            {
                var distanceFromZero = (LowestCharOffsetContained - newMinimum) / 2;
                var newBytes = new byte[newSize];
                Array.Copy(
                    sourceArray: WorkingMemory,
                    sourceIndex: distanceFromZero,
                    destinationArray: newBytes,
                    destinationIndex: 0,
                    length: newBytes.Length);
                this.WorkingMemory = newBytes;
            }
        }

        public void Dispose()
        {
            this.mutex.WaitOne();
            this.mutex.Dispose();
            this.WorkingMemory = null;
        }
    }
}
