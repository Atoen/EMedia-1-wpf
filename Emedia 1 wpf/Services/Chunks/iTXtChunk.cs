using System.Text;
using System.Text.RegularExpressions;

namespace Emedia_1_wpf.Services.Chunks;

public class iTXtChunk : PngChunk
{
    public string Keyword { get; }
    public bool Compressed { get; }
    public CompressionMethod CompressionMethod { get; }
    public string LanguageTag { get; }
    public string TranslatedKeyword { get; }
    public string Text { get; }

    public string AdditionalData { get; }

    private static readonly char[] separator = new[] { '\n' };

    public iTXtChunk(uint length, byte[] data, string type, uint crc, bool crcValid) :
        base(length, data, type, crc, crcValid)
    {
        var firstNullIndex = Array.IndexOf(data, (byte) 0);
        var secondNullIndex = Array.IndexOf(data, (byte) 0, firstNullIndex + 3);
        var thirdNullIndex = Array.IndexOf(data, (byte) 0, secondNullIndex + 1);
        if (firstNullIndex < 0 || secondNullIndex < 0 || thirdNullIndex < 0)
        {
            throw new ChunkException(PngChunkType.iTXt,"iTXt chunk: missing null terminator");
        }

        Keyword = Encoding.ASCII.GetString(data, 0, firstNullIndex);
        Compressed = data[firstNullIndex + 1] != 0;
        CompressionMethod = (CompressionMethod) data[firstNullIndex + 2];

        var languageTagLength = secondNullIndex - (firstNullIndex + 3);
        LanguageTag = Encoding.ASCII.GetString(data, firstNullIndex + 3, languageTagLength);

        var translatedKeywordLength = thirdNullIndex - (secondNullIndex + 1);
        TranslatedKeyword = Encoding.UTF8.GetString(data, secondNullIndex, translatedKeywordLength);

        var textLength = data.Length - (thirdNullIndex + 1);
        Text = Encoding.UTF8.GetString(data, thirdNullIndex + 1, textLength).Replace("\n","");

        AdditionalData = Text;

        var pattern = @"(<\/?(exif:|tiff:)|<[^>]+>)";
        var cleanXmlString = Regex.Replace(AdditionalData, pattern, match =>
        {
            if (!match.Groups[1].Success) return match.Value;
            return match.Groups[2].Value is "exif:" or "tiff:" ? match.Value : "";
        });

        AdditionalData = Regex.Replace(cleanXmlString, @"\s+", " ")
            .Replace("> <", "\n")
            [2..]
            [..^1]
            .Replace(" ", "")
            .Replace("</", " ")
            .Replace(">", " ")
            .Replace("tiff:", "")
            .Replace("exif:", "");
        
        AdditionalData = "\n" + RemoveAfterLastSpace(AdditionalData);
        // Console.WriteLine(additionalData);
    }

    static string RemoveAfterLastSpace(string input)
    {
        var lines = input.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].Trim();
            var lastSpaceIndex = lines[i].LastIndexOf(' ');
            
            if (lastSpaceIndex != -1)
            {
                lines[i] = lines[i][..lastSpaceIndex];
            }
        }
        return string.Join("\n", lines);
    }
    
    public override string FormatData()
    {
        var builder = new StringBuilder();

        builder.Append($"Type: {Type}");
        if (Keyword != string.Empty)
        {
            builder.Append($" Keyword: {Keyword}");
        }

        builder.Append($" Compressed: {Compressed}");
        if (Compressed)
        {
            builder.Append($" Compression method: {CompressionMethod}");
        }
        
        if (LanguageTag != string.Empty)
        {
            builder.Append($" Language tag: {LanguageTag}");
        }
        if (TranslatedKeyword != string.Empty)
        {
            builder.Append($" Translated keyword: {TranslatedKeyword}");
        }

        builder.Append("Remaining metadata:");
        builder.Append(Text.Length < 100 ? Text : $"{Text[..100]}...");

        return builder.ToString();
    }
}