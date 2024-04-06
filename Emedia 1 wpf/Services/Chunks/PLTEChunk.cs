namespace Emedia_1_wpf.Services.Chunks;

public class PLTEChunk : PngChunk
{
    public byte[] Palette { get; }

    public override bool RemoveWhenAnonymizing => false;

    public PLTEChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        Palette = data;
    }

    public override string FormatData() => $"Type: {Type}, Palette Length: {Palette.Length / 3} colors";

    protected override void EnsureValid()
    {
        if (Length % 3 != 0)
        {
            throw new ChunkException(PngChunkType.PLTE,"Invalid PLTE chunk data length.");
        }
    }
}