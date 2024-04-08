using System.Text;

namespace Emedia_1_wpf.Services.Chunks;

public class eXIfChunk : PngChunk
{
    public eXIfChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
    }

    public override string FormatData() => $"Type: {Type}, eXIf data length {Data.Length}";
}
