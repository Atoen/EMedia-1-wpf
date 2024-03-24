namespace Emedia_1_wpf.Services.Chunks;

public class IENDChunk : PngChunk
{
    public override bool RemoveWhenAnonymizing => false;

    public IENDChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
    }

    public override string FormatData() => $"Type: {Type}";
}