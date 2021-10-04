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

        private static string NSizeKey(long n, int blockSize) => string.Format("n{0}:b{1}", n, blockSize);
        private static string NSizeValue(char firstDigit, string sha256) => string.Format("{0}:{1}", firstDigit, sha256.ToLowerInvariant());
        private static (char, string) NSizeValue(string storedValue)
        {
            var parts = storedValue.Split(
                separator: new char[] { ':' },
                count: 2);
            return (parts[0][0], parts[1]);
        }

        private static string SizeSHAKey(int blockSize, string sha256) => string.Format("b{0}:s{1}", blockSize, sha256.ToLowerInvariant());
        private static string SizeSHAValue(long nOffset, string oldValue = null)
        {
            var nOffsetString = Convert.ToString(nOffset);
            return oldValue is null ? nOffsetString : string.Join(
                separator: ",",
                oldValue,
                nOffsetString);
        }
        private static IEnumerable<long> SizeSHAValue(string storedValue) =>
            storedValue
                .Split(',')
                .Select(value => Convert.ToInt64(value));

        private static string NextSizeKey(int blockSize) => string.Format("nextBlock:{0}", blockSize);
        private static string NextSizeValue(long n) => string.Format("{0}", n);
        private static long NextSizeValue(string storedValue) => Convert.ToInt64(storedValue);

        private long GetNextN(int blockSize, ClientSession<string, string, string, string, object, IFunctions<string, string, string, string, object>>? session = null)
        {
            if (session is null)
            {
                fasterPiStore.NewSession(
                    functions: new SimpleFunctions<string, string>());
            }

            var nextSizeTuple = session.Read(
                key: NextSizeKey(
                    blockSize: blockSize));
            return nextSizeTuple.status == Status.OK ? NextSizeValue(nextSizeTuple.output) : 0;
        }

        private void SetNextN(int blockSize, long nextN, ClientSession<string, string, string, string, object, IFunctions<string, string, string, string, object>>? session = null)
        {
            if (session is null)
            {
                fasterPiStore.NewSession(
                    functions: new SimpleFunctions<string, string>());
            }

            session.Upsert(
                key: NextSizeKey(blockSize: blockSize),
                desiredValue: NextSizeValue(n: nextN));
        }

        public void AddComputation(long n, int blockSize, char firstChar, string sha256)
        {
            var sizeSHAKey = SizeSHAKey(
                    blockSize: blockSize,
                    sha256: sha256);

            var session = fasterPiStore.NewSession(
                functions: new SimpleFunctions<string, string, object>());

            // read
            var sizeSHAOldValueTuple = session.Read(
                key: sizeSHAKey);
            string sizeSHAOldValue = (sizeSHAOldValueTuple.status == Status.OK) ? sizeSHAOldValueTuple.output : null;

            // update
            session.Upsert(
                key: NSizeKey(
                    n: n,
                    blockSize: blockSize),
                desiredValue: NSizeValue(
                    firstDigit: firstChar,
                    sha256: sha256));
            session.Upsert(
                key: sizeSHAKey,
                desiredValue: SizeSHAValue(nOffset: n));
            session.CompletePending(
                wait: false);
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

        private DirectoryInfo GetDiskCacheDirectory()
        {
            return Directory.CreateDirectory(
                Path.Combine(
                    path1: this.baseDirectory.FullName,
                    path2: "PiMiner"));
        }

        private string GetDevicePath(string nameSpace, out DirectoryInfo cacheDirectoryInfo)
        {
            cacheDirectoryInfo = this.GetDiskCacheDirectory();

            return Path.Combine(
                cacheDirectoryInfo.FullName,
                string.Format(
                    provider: System.Globalization.CultureInfo.InvariantCulture,
                    format: "{0}.log",
                    nameSpace));
        }

        private IDevice CreateLogDevice(string nameSpace)
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
