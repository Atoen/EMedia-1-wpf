using Emedia_1_wpf.Extensions;

namespace Emedia_1_wpf.Services.Chunks;

public class cHRMChunk : PngChunk
{
    public double WhitePointX { get; }
    public double WhitePointY { get; }
    public double RedX { get; }
    public double RedY { get; }
    public double GreenX { get; }
    public double GreenY { get; }
    public double BlueX { get; }
    public double BlueY { get; }

    public cHRMChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        var span = data.AsSpan();
        
        WhitePointX = span[..4].GetFixedPoint();
        WhitePointY = span[4..8].GetFixedPoint();
        RedX = span[8..12].GetFixedPoint();
        RedY = span[12..16].GetFixedPoint();
        GreenX = span[16..20].GetFixedPoint();
        GreenY = span[20..24].GetFixedPoint();
        BlueX = span[24..28].GetFixedPoint();
        BlueY = span[28..32].GetFixedPoint();
    }

    public override string FormatData() => $"Type: {Type}, White Point: ({WhitePointX}, {WhitePointY}), Red: ({RedX}, {RedY}), Green: ({GreenX}, {GreenY}), Blue: ({BlueX}, {BlueY})";

    protected override void EnsureValid()
    {
        if (Data.Length != 32)
        {
            throw new ChunkException(PngChunkType.cHRM,"Invalid cHRM chunk data length.");
        }
    }
}