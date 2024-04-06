namespace Emedia_1_wpf.Services.Chunks;

public class sBITChunk : PngChunk
{
    public byte[] SignificantBits { get; }

    public sBITChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        SignificantBits = data;
    }
    
    public override string FormatData() => $"Type: {Type}, Significant Bits Length: {SignificantBits.Length}";

    protected override void EnsureValid()
    {
        if (Data.Length != 1)
        {
            throw new ChunkException(PngChunkType.sBIT,"sBIT chunk data must be exactly 1 byte long.");
        }
    }
}
