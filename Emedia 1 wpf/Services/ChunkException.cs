using Emedia_1_wpf.Services.Chunks;

namespace Emedia_1_wpf.Services;

public class ChunkException(PngChunkType chunkType, string message) : Exception(message)
{
    public PngChunkType ChunkType { get; } = chunkType;
}