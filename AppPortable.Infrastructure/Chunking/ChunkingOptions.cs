namespace AppPortable.Infrastructure.Chunking;

public sealed class ChunkingOptions
{
    public int TargetCharacters { get; set; } = 3600;
    public int MaxCharacters { get; set; } = 4800;
    public double OverlapRatio { get; set; } = 0.1;
    public int MinChunkLength { get; set; } = 100;
}
