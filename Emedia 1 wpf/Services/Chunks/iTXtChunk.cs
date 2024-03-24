using System.Text;

namespace Emedia_1_wpf.Services.Chunks;

public class iTXtChunk : PngChunk
{
    public string Keyword { get; }
    public bool CompressionFlag { get; }
    public byte CompressionMethod { get; }
    public string LanguageTag { get; }
    public string TranslatedKeyword { get; }
    public string Text { get; }
    
    public override bool RemoveWhenAnonymizing => true;


    public iTXtChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        var nullIndex = Array.IndexOf(data, (byte)0);
        if (nullIndex < 0)
        {
            throw new ArgumentException("iTXt chunk bad: no null terminator(keyword)");
        }
        
        Keyword = Encoding.ASCII.GetString(data, 0, nullIndex);
        var offset = nullIndex + 1;
        if (offset + 5 >= data.Length)
        {
            throw new ArgumentException("iTXt chunk bad: data length too short");
        }
        
        CompressionFlag = data[offset] != 0;
        CompressionMethod = data[offset + 1];
        LanguageTag = Encoding.ASCII.GetString(data, offset + 2, 3);
        var translatedKeywordNullIndex = Array.IndexOf(data, (byte)0, offset + 5);
        if (translatedKeywordNullIndex < 0)
        {
            throw new ArgumentException("iTXt chunk bad: no null terminator(trans keyword)");
        }
        
        TranslatedKeyword = Encoding.ASCII.GetString(data, offset + 5, translatedKeywordNullIndex - (offset + 5));
        var textStartIndex = translatedKeywordNullIndex + 1;
        if (textStartIndex >= data.Length)
        {
            throw new ArgumentException("iTXt chunk bad: no text data");
        }
        
        Text = Encoding.Latin1.GetString(data, textStartIndex, data.Length - textStartIndex);
    }

    public override string FormatData()
    {
        return $"Type: {Type}";
        // Console.WriteLine($"\tKeyword: {Keyword}");
        // Console.WriteLine($"\tCompression Flag: {(CompressionFlag ? "Compressed" : "Uncompressed")}");
        // Console.WriteLine($"\tCompression Method: {CompressionMethod}");
        // Console.WriteLine($"\tLanguage Tag: {LanguageTag}");
        // Console.WriteLine($"\tTranslated Keyword: {TranslatedKeyword}");
        // Console.WriteLine($"\tText: {Text}");
    }
}