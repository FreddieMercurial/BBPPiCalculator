using NeuralFabric.Models.Hashes;

namespace BBP;

[Serializable]
public record PiBlock(long N, ReadOnlyMemory<byte> Values, DataHash DataHash);
