using System.Text;

namespace Emedia_1_wpf.Services.Chunks;

public class zTXtChunk : PngChunk
{
    public string Keyword { get; }
    public CompressionMethod CompressionMethod { get; }
    public string Text { get; }
    
    public override bool RemoveWhenAnonymizing => true;

    public zTXtChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        var span = data.AsSpan();
        var nullIndex = span.IndexOf((byte) 0);

        Keyword = Encoding.Latin1.GetString(span[..nullIndex]);
        CompressionMethod = (CompressionMethod) span[nullIndex + 1];
        Text = DecompressString(data[(nullIndex + 4)..], Encoding.Latin1); // 1 after compressionMethod + magic offset of 2
    }

    public override string FormatData() => $"Type: {Type}, Keyword: {Keyword}, Compression Method: {CompressionMethod}, Text: {Text}";
}
