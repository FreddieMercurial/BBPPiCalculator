using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using FASTER.core;

namespace BBP;

public class FasterKVBBPiMiner : IDisposable
{
    /// <summary>
    ///     hash table size (number of 64-byte buckets).
    /// </summary>
    private const long HashTableBuckets = 1L << 20;

    /// <summary>
    ///     Directory where the block tree root will be placed.
    /// </summary>
    private readonly DirectoryInfo baseDirectory;

    private readonly IDevice fasterPiLogDevice;
    private readonly IDevice fasterPiObjectDevice;
    private readonly FasterKV<string, string> fasterPiStore;

    /// <summary>
    ///     Whether we enable a read cache.
    ///     Updated from config.
    /// </summary>
    private readonly bool useReadCache = false;

    [SuppressMessage(category: "ReSharper.DPA",
        checkId: "DPA0003: Excessive memory allocations in LOH",
        MessageId = "type: FASTER.core.HashBucket[]")]
    public FasterKVBBPiMiner(string baseDirectory)
    {
        this.baseDirectory = new DirectoryInfo(path: baseDirectory);
        var cacheDir = GetDiskCacheDirectory(baseDirectory: this.baseDirectory).FullName;
        fasterPiLogDevice = CreateLogDevice(baseDirectory: this.baseDirectory,
            nameSpace: "pi-log");
        fasterPiObjectDevice = CreateLogDevice(baseDirectory: this.baseDirectory,
            nameSpace: "pi-object");
        // KEY                          VALUE
        // (n, blockSize)               (firstDigit, sha256 of block)
        // (blockSize, sha256)          (n-offset,n-offset,...)
        // ('nextBlock', blockSize)     next n to process for this size
        fasterPiStore = new FasterKV<string, string>(
            size: HashTableBuckets,
            logSettings: NewLogSettings(
                logDevice: fasterPiLogDevice,
                objectDevice: fasterPiObjectDevice,
                useReadCache: useReadCache),
            checkpointSettings: NewCheckpointSettings(cacheDir: cacheDir),
            serializerSettings: null,
            comparer: null);
    }

    public void Dispose()
    {
        fasterPiStore.TakeFullCheckpoint(token: out _);
        fasterPiStore.Dispose();
        fasterPiObjectDevice.Dispose();
        fasterPiLogDevice.Dispose();
    }

    private static string NSizeKey(long n, int blockSize)
    {
        return $"n{n}:b{blockSize}";
    }

    private static string NSizeValue(byte firstByte, string sha256)
    {
        return $"{firstByte:X}:{sha256.ToLowerInvariant()}";
    }

    private static (char, string) NSizeValue(string storedValue)
    {
        var parts = storedValue.Split(
            separator: new[] {':'},
            count: 2);
        return (parts[0][index: 0], parts[1]);
    }

    private static string SizeShaKey(int blockSize, string sha256)
    {
        return $"b{blockSize}:s{sha256.ToLowerInvariant()}";
    }

    private static string SizeShaValue(long nOffset, string? oldValue = null)
    {
        var nOffsetString = Convert.ToString(value: nOffset);
        return oldValue is null
            ? nOffsetString
            : string.Join(
                separator: ",",
                oldValue,
                nOffsetString);
    }

    private static IEnumerable<long> SizeSHAValue(string storedValue)
    {
        return storedValue
            .Split(separator: ',')
            .Select(selector: value => Convert.ToInt64(value: value));
    }

    private static string NextSizeKey(int blockSize)
    {
        return $"nextBlock:{blockSize}";
    }

    private static string NextSizeValue(long n)
    {
        return $"{n}";
    }

    private static long NextSizeValue(string storedValue)
    {
        return Convert.ToInt64(value: storedValue);
    }

    private long GetNextN(int blockSize,
        ClientSession<string, string, string, string, object, IFunctions<string, string, string, string, object>>? session = null)
    {
        session ??= fasterPiStore.NewSession(
            functions: new SimpleFunctions<string, string, object>());

        var (status, output) = session.Read(
            key: NextSizeKey(
                blockSize: blockSize));

        return status == Status.OK ? NextSizeValue(storedValue: output) : 0;
    }

    private void SetNextN(int blockSize, long nextN,
        ClientSession<string, string, string, string, object, IFunctions<string, string, string, string, object>>? session = null)
    {
        session ??= fasterPiStore.NewSession(functions: new SimpleFunctions<string, string, object>());
        session!.Upsert(
            key: NextSizeKey(blockSize: blockSize),
            desiredValue: NextSizeValue(n: nextN));
    }

    public void AddComputation(long n, int blockSize, byte firstByte, string sha256)
    {
        var sizeShaKey = SizeShaKey(
            blockSize: blockSize,
            sha256: sha256);

        var session = fasterPiStore.NewSession(
            functions: new SimpleFunctions<string, string, object>());

        // read
        var (status, output) = session.Read(
            key: sizeShaKey);
        var sizeShaOldValue = status == Status.OK ? output : null;

        // update
        session.Upsert(
            key: NSizeKey(
                n: n,
                blockSize: blockSize),
            desiredValue: NSizeValue(
                firstByte: firstByte,
                sha256: sha256));
        session.Upsert(
            key: sizeShaKey,
            desiredValue: SizeShaValue(nOffset: n));
        session.CompletePending(
            wait: false);
    }

    private static LogSettings NewLogSettings(IDevice logDevice, IDevice objectDevice, bool useReadCache)
    {
        return new LogSettings
        {
            LogDevice = logDevice, ObjectLogDevice = objectDevice, ReadCacheSettings = useReadCache ? new ReadCacheSettings() : null,
        };
    }

    private static CheckpointSettings NewCheckpointSettings(string cacheDir)
    {
        return new CheckpointSettings {CheckpointDir = cacheDir};
    }

    private static DirectoryInfo EnsuredDirectory(string dir)
    {
        return !Directory.Exists(path: dir) ? Directory.CreateDirectory(path: dir) : new DirectoryInfo(path: dir);
    }

    private static DirectoryInfo GetDiskCacheDirectory(DirectoryInfo baseDirectory)
    {
        return Directory.CreateDirectory(
            path: Path.Combine(
                path1: baseDirectory.FullName,
                path2: "PiMiner"));
    }

    private static string GetDevicePath(DirectoryInfo baseDirectory, string nameSpace, out DirectoryInfo cacheDirectoryInfo)
    {
        cacheDirectoryInfo = GetDiskCacheDirectory(baseDirectory: baseDirectory);

        return Path.Combine(
            path1: cacheDirectoryInfo.FullName,
            path2: string.Format(
                provider: CultureInfo.InvariantCulture,
                format: "{0}.log",
                arg0: nameSpace));
    }

    private static IDevice CreateLogDevice(DirectoryInfo baseDirectory, string nameSpace)
    {
        var devicePath = GetDevicePath(
            baseDirectory: baseDirectory,
            nameSpace: nameSpace,
            cacheDirectoryInfo: out var _);

        return Devices.CreateLogDevice(
            logPath: devicePath);
    }
}
