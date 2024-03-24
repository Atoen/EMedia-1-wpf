namespace Emedia_1_wpf.Services.Chunks;

public class IDATChunk : PngChunk
{
    public byte[] ImageData { get; }

    public override bool RemoveWhenAnonymizing => false;

    public IDATChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        ImageData = data;
    }

    public override string FormatData() => $"Type: {Type}, Data Length: {ImageData.Length}";
}