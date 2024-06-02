using System.IO;
using Emedia_1_wpf.Models;
using Emedia_1_wpf.Services.Chunks;

namespace Emedia_1_wpf.Services;

public class PngService
{
    public static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    public static int SignatureLength => PngSignature.Length;
    
    private readonly CryptoService _cryptoService = new();
    
    public async Task EncryptAsync(
        List<PngChunk> chunks,
        string filePath,
        CryptographyMode cryptographyMode,
        IProgress<double>? progress = null)
    {
        await using var stream = File.Create(filePath);
        
        var decoded = await GetDecodedDataAsync(chunks);
        var encrypted = await _cryptoService.EncryptAsync(decoded, cryptographyMode, progress);

        await SavePngImageAsync(stream, chunks, encrypted);
    }
    
    public async Task DecryptAsync(
        List<PngChunk> chunks,
        string filePath,
        CryptographyMode cryptographyMode,
        IProgress<double>? progress = null)
    {
        await using var stream = File.Create(filePath);
        
        var decoded = await GetDecodedDataAsync(chunks);
        var decrypted = await _cryptoService.DecryptAsync(decoded, cryptographyMode, progress);

        await SavePngImageAsync(stream, chunks, decrypted);
    }

    private async Task<byte[]> GetDecodedDataAsync(List<PngChunk> chunks)
    {
        var header = (IHDRChunk) chunks[0];
        var filter = new PngFilter(header.Width, header.ColorType, header.BitDepth);
        
        var data = chunks.Where(x => x is IDATChunk)
            .SelectMany(x => x.Data)
            .Skip(2)
            .ToArray();
        
        var decompressed = await PngChunk.DecompressAsync(data);
        var decoded = decompressed.Chunk(header.Width * filter.PixelWidth + 1)
            .SelectMany(x => filter.Decode(x))
            .ToArray();

        return decoded;
    }

    private async Task SavePngImageAsync(Stream stream, List<PngChunk> chunks, IEnumerable<byte> pixelData)
    {
        var header = (IHDRChunk) chunks[0];
        var filter = new PngFilter(header.Width, header.ColorType, header.BitDepth);

        var encoded = pixelData.Chunk(header.Width * filter.PixelWidth)
            .SelectMany(x => filter.EncodeNone(x))
            .ToArray();

        var compressed = await PngChunk.CompressAsync(encoded);
        var encryptedChunk = IDATChunk.FromBytes(compressed);
        
        var indexOfData = chunks.FindIndex(x => x is IDATChunk);
        var otherChunks = chunks.Where(x => x is not IDATChunk)
            .ToArray();

        await stream.WriteAsync(PngSignature);
        for (var i = 0; i < otherChunks.Length; i++)
        {
            var chunk = otherChunks[i];
            if (i == indexOfData)
            {
                await encryptedChunk.AppendToStreamAsync(stream);
            }

            await chunk.AppendToStreamAsync(stream);
        }
    }
    
    public async ValueTask<bool> VerifyPngAsync(string filePath)
    {
        try
        {
            await using var stream = File.Open(filePath, FileMode.Open);
            return await CheckSignatureAsync(stream);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<List<PngChunk>> ReadDataAsync(string filePath, Action<Log>? callback = null)
    {
        await using var stream = File.Open(filePath, FileMode.Open);
        stream.Seek(SignatureLength, SeekOrigin.Begin);
        
        var chunks = new List<PngChunk>();
        var chunk = default(PngChunk);
        do
        {
            try
            {
                chunk = await PngChunk.CreateAsync(stream);
                callback?.Invoke(new Log(LogType.Info, chunk.FormatData()));
                
                chunks.Add(chunk);
            }
            catch (ChunkException e)
            {
                callback?.Invoke(new Log(LogType.Warning, $"{e.ChunkType}: {e.Message}"));
            }
            catch (Exception e)
            {
                callback?.Invoke(new Log(LogType.Error, $"Error while reading image: {e.Message}"));
            }
        } while (chunk is not IENDChunk);

        return chunks;
    }

    public async Task AnonymizeImageAsync(List<PngChunk> chunks, string filepath)
    {
        await using var stream = File.Create(filepath);
        
        var chunksToSave = chunks.Where(x => !x.RemoveWhenAnonymizing)
            .ToArray();
        
        var data = chunksToSave.Where(x => x is IDATChunk)
            .SelectMany(x => x.Data)
            .ToArray();

        var dataChunk = IDATChunk.FromBytes(data);
        if (chunksToSave is not [IHDRChunk header, .., IENDChunk end])
        {
            throw new InvalidOperationException();
        }
        
        await stream.WriteAsync(PngSignature);
        await header.AppendToStreamAsync(stream);
        if (chunksToSave.SingleOrDefault(x => x is PLTEChunk) is { } palette)
        {
            await palette.AppendToStreamAsync(stream);
        }
        
        await dataChunk.AppendToStreamAsync(stream);
        await end.AppendToStreamAsync(stream);
    }
    
    private async ValueTask<bool> CheckSignatureAsync(Stream stream)
    {
        var buffer = new byte[SignatureLength];
        var bytesRead = await stream.ReadAsync(buffer);

        return buffer.Take(bytesRead)
            .SequenceEqual(PngSignature);
    }
}