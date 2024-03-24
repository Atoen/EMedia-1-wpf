namespace Emedia_1_wpf.Services.Chunks;

public class tIMEChunk : PngChunk
{
    public DateTime LastModificationTime { get; }

    public override bool RemoveWhenAnonymizing => true;

    public tIMEChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        LastModificationTime = new DateTime(data[0] << 8 | data[1], // Year
            data[2],                // Month
            data[3],                // Day
            data[4],                // Hour
            data[5],                // Minute
            data[6]);               // Second
    }

    public override string FormatData() => $"Type: {Type}, Last Modification Time: {LastModificationTime}";

    protected override void EnsureValid()
    {
        if (Data.Length != 7)
        {
            throw new ArgumentException("Invalid tIME chunk data length.");
        }
    }
}