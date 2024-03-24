using System.Text;

namespace Emedia_1_wpf.Services.Chunks;

public class sPLTChunk : PngChunk
{
    public string PaletteName { get; }
    public byte SampleDepth { get; }

    public byte[] SampleInfo { get; }

    public virtual bool AllowMultiple => true;
    
    
    public sPLTChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        var nullIndex = Array.IndexOf(data, (byte)0);

        PaletteName = Encoding.Latin1.GetString(data, 0, nullIndex);

        SampleDepth = data[nullIndex + 1];

        SampleInfo = data[(nullIndex + 2)..];
    }

    public override string FormatData() =>
        $"Type: {Type}, Palette Name: {PaletteName}, Sample Depth: {SampleDepth}, Sample Info Length: {SampleInfo.Length}";
}


