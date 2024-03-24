namespace Emedia_1_wpf.Services.Chunks;

public class tRNSChunk : PngChunk
{
    public byte[] TransparencyData { get; }

    public tRNSChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        TransparencyData = data;
    }

    public override string FormatData() => $"Type: {Type}, Data Length: {TransparencyData.Length}";
}