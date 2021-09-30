namespace BBP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using FASTER.core;

    public class FasterKVBBPiMiner : IDisposable
    {
        private readonly IDevice fasterPiLogDevice;
        private readonly IDevice fasterPiObjectDevice;
        private readonly FasterKV<string, string> fasterPiStore;

        /// <summary>
        /// hash table size (number of 64-byte buckets).
        /// </summary>
        private const long HashTableBuckets = 1L << 20;

        /// <summary>
        ///     Directory where the block tree root will be placed.
        /// </summary>
        private readonly DirectoryInfo baseDirectory;

        /// <summary>
        /// Whether we enable a read cache.
        /// Updated from config.
        /// </summary>
        private readonly bool useReadCache = false;

        public FasterKVBBPiMiner()
        {

            var cacheDir = this.GetDiskCacheDirectory().FullName;
            this.fasterPiLogDevice = this.CreateLogDevice("pi-log");
            this.fasterPiObjectDevice = this.CreateLogDevice("pi-object");
            // KEY                          VALUE
            // (n, blockSize)               (firstDigit, sha256 of block)
            // (blockSize, sha256)          (n-offset,n-offset,...)
            // ('nextBlock', blockSize)     next n to process for this size
            this.fasterPiStore = new FasterKV<string, string>(
                            size: HashTableBuckets,
                            logSettings: NewLogSettings(
                                logDevice: this.fasterPiLogDevice,
                                objectDevice: this.fasterPiObjectDevice,
                                useReadCache: useReadCache),
                            checkpointSettings: NewCheckpointSettings(cacheDir),
                            serializerSettings: null,
                            comparer: null);
        }

        public static string NSizeKey(int n, int blockSize) => string.Format("n{0}:b{1}", n, blockSize);
        public static string NSizeValue(char firstDigit, string sha256) => string.Format("{0}:{1}", firstDigit, sha256);
        public static (char, string) NSizeValue(string storedValue)
        {
            var parts = storedValue.Split(
                separator: new char[] { ':' },
                count: 2);
            return (parts[0][0], parts[1]);
        }

        public static string SizeSHAKey(int blockSize, string sha256) => string.Format("b{0}:s{1}", blockSize, sha256);
        public static string SizeSHAValue(int nOffset, string oldValue = null)
        {
            var nOffsetString = Convert.ToString(nOffset);
            return oldValue is null ? nOffsetString : string.Join(
                separator: ",",
                oldValue,
                nOffsetString);
        }
        public static IEnumerable<int> SizeSHAValue(string storedValue) =>
            storedValue
                .Split(',')
                .Select(value => Convert.ToInt32(value));

        public static string NextSizeKey(int blockSize) => string.Format("nextBlock:{0}", blockSize);
        public static string NextSizeValue(int n) => string.Format("{0}", n);
        public static int NextSizeValue(string storedValue) => Convert.ToInt32(storedValue);

        public int AddComputation(int n, int blockSize, char firstDigit, string sha256)
        {
            var nextSizeKey = NextSizeKey(blockSize: blockSize);
            var sizeSHAKey = SizeSHAKey(
                    blockSize: blockSize,
                    sha256: sha256);

            var session = fasterPiStore.NewSession(
                functions: new AdvancedSimpleFunctions<string, string>());

            // read
            var sizeSHAOldValueTuple = session.Read(
                key: sizeSHAKey);
            string sizeSHAOldValue = (sizeSHAOldValueTuple.status == Status.OK) ? sizeSHAOldValueTuple.output : null;

            var nextSizeTuple = session.Read(
                key: nextSizeKey);
            int nextN = nextSizeTuple.status == Status.OK ? NextSizeValue(nextSizeTuple.output) : n + 1;

            // update
            session.Upsert(
                key: NSizeKey(
                    n: n,
                    blockSize: blockSize),
                desiredValue: NSizeValue(
                    firstDigit: firstDigit,
                    sha256: sha256));
            session.Upsert(
                key: sizeSHAKey,
                desiredValue: SizeSHAValue(nOffset: n));
            session.Upsert(
                key: nextSizeKey,
                desiredValue: NextSizeValue(n: nextN));
            session.CompletePending(
                wait: false);
            return nextN;
        }

        private static LogSettings NewLogSettings(IDevice logDevice, IDevice objectDevice, bool useReadCache)
        {
            return new LogSettings
            {
                LogDevice = logDevice,
                ObjectLogDevice = objectDevice,
                ReadCacheSettings = useReadCache ? new ReadCacheSettings() : null,
            };
        }

        private static CheckpointSettings NewCheckpointSettings(string cacheDir)
        {
            return new CheckpointSettings
            {
                CheckpointDir = cacheDir,
            };
        }

        private DirectoryInfo EnsuredDirectory(string dir)
        {
            if (!Directory.Exists(dir))
            {
                return Directory.CreateDirectory(dir);
            }

            return new DirectoryInfo(dir);
        }

        protected DirectoryInfo GetDiskCacheDirectory()
        {
            return Directory.CreateDirectory(
                Path.Combine(
                    path1: this.baseDirectory.FullName,
                    path2: "PiMiner"));
        }

        protected string GetDevicePath(string nameSpace, out DirectoryInfo cacheDirectoryInfo)
        {
            cacheDirectoryInfo = this.GetDiskCacheDirectory();

            return Path.Combine(
                cacheDirectoryInfo.FullName,
                string.Format(
                    provider: System.Globalization.CultureInfo.InvariantCulture,
                    format: "{0}.log",
                    nameSpace));
        }

        protected IDevice CreateLogDevice(string nameSpace)
        {
            var devicePath = this.GetDevicePath(nameSpace, out DirectoryInfo _);

            return Devices.CreateLogDevice(
                logPath: devicePath);
        }

        public void Dispose()
        {
            this.fasterPiStore.TakeFullCheckpoint(out _);
            this.fasterPiStore.Dispose();
            this.fasterPiObjectDevice.Dispose();
            this.fasterPiLogDevice.Dispose();
        }
    }
}
