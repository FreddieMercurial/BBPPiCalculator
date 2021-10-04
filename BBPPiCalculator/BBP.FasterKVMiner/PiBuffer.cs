namespace BBP.FasterKVMiner
{
    public class PiBuffer : IDisposable
    {
        private PiDigit piGenerator;
        private long LowestOffsetContained;
        private long HighestOffsetContained;
        private byte[] WorkingMemory;
        private readonly int MaximumCapacity;
        private Mutex mutex;

        public PiBuffer(long startingOffset, int maxCapacity)
        {
            if (startingOffset < 0)
            {
                throw new ArgumentException(nameof(startingOffset));
            }

            this.MaximumCapacity = maxCapacity;
            this.piGenerator = new PiDigit(nOffset: startingOffset);
            this.LowestOffsetContained = 1;
            this.HighestOffsetContained = 10;
            var memorySize = (int)(HighestOffsetContained - LowestOffsetContained);
            this.WorkingMemory = piGenerator.PiBytes(
                n: LowestOffsetContained,
                count: memorySize).ToArray();
            this.mutex = new Mutex(false);
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
            else if (
                (minimum >= LowestOffsetContained) &&
                (minimum <= HighestOffsetContained) &&
                (maximum >= LowestOffsetContained) &&
                (maximum <= HighestOffsetContained)
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

                if (minimum < LowestOffsetContained)
                {

                    // add bytes on the left
                    var bytesNeeded = (int)(LowestOffsetContained - minimum);
                    var leftMemory = this.piGenerator.PiBytes(
                        n: minimum,
                        count: bytesNeeded).ToArray();
                    Array.Resize(ref leftMemory, bytesNeeded + WorkingMemory.Length);
                    Array.Copy(
                        sourceArray: WorkingMemory,
                        sourceIndex: 0,
                        destinationArray: leftMemory,
                        destinationIndex: bytesNeeded,
                        length: WorkingMemory.Length);
                    this.WorkingMemory = leftMemory;
                    this.LowestOffsetContained = minimum;
                }

                if (maximum > HighestOffsetContained)
                {
                    var bytesNeeded = (int)(maximum - HighestOffsetContained);
                    var rightMemory = this.piGenerator.PiBytes(
                        n: this.HighestOffsetContained + 1,
                        count: bytesNeeded).ToArray();
                    var oldLength = WorkingMemory.Length;
                    Array.Resize(ref WorkingMemory, oldLength + bytesNeeded);
                    Array.Copy(
                        sourceArray: rightMemory,
                        sourceIndex: 0,
                        destinationArray: WorkingMemory,
                        destinationIndex: oldLength,
                        length: bytesNeeded);
                    this.HighestOffsetContained = maximum;
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
            var returnArray = new byte[maximum - minimum];
            try
            {
                this.mutex.WaitOne();

                this.Ensure(
                    minimum: minimum,
                    maximum: maximum,
                    useMutex: false);
                Array.Copy(
                    sourceArray: WorkingMemory,
                    sourceIndex: minimum - LowestOffsetContained,
                    destinationArray: returnArray,
                    destinationIndex: 0,
                    length: returnArray.Length);
            }
            finally
            {
                this.mutex.ReleaseMutex();
            }
            return returnArray;
        }

        private void GarbageCollect(long requestedMinimum, long requestedMaximum)
        {
            var sizeRequested = requestedMaximum - requestedMinimum;
            if (sizeRequested > MaximumCapacity)
            {
                this.PruneLeft(newMinimum: requestedMaximum);
            }
        }

        private void PruneLeft(long newMinimum)
        {
            if ((newMinimum < LowestOffsetContained) || (newMinimum > HighestOffsetContained))
            {
                throw new ArgumentException(nameof(newMinimum));
            }
            var bytesToRemove = newMinimum - LowestOffsetContained;
            var newSize = this.WorkingMemory.Length - bytesToRemove;
            if (bytesToRemove > 0)
            {
                var newBytes = new byte[newSize];
                Array.Copy(
                    sourceArray: WorkingMemory,
                    sourceIndex: newMinimum,
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
