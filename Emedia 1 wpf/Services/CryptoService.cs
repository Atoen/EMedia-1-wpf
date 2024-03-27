using System.Security.Cryptography;

namespace Emedia_1_wpf.Services;

public class CryptoService
{
    private static readonly RSACryptoServiceProvider CryptoServiceProvider = new(2048);
    private static readonly RSAParameters PrivateKey = CryptoServiceProvider.ExportParameters(true);
    private static readonly RSAParameters PublicKey = CryptoServiceProvider.ExportParameters(false);

    private static readonly double UpdateThreshold = 0.005;
    
    public async Task<byte[]> EncryptAsync(byte[] data, bool useLibrary, IProgress<double>? progress = null)
    {
        using var csp = new RSACryptoServiceProvider();
        csp.ImportParameters(PublicKey);

        return await Task.Run(() => Encrypt(data, useLibrary, progress));
    }
    
    public async Task<byte[]> DecryptAsync(byte[] encryptedData, bool useLibrary, IProgress<double>? progress = null)
    {
        using var csp = new RSACryptoServiceProvider();
        csp.ImportParameters(PrivateKey);
        
        return await Task.Run(() => Decrypt(encryptedData, useLibrary, progress));
    }
    
    public byte[] Encrypt(byte[] data, bool useLibrary, IProgress<double>? progress = null)
    {
        using var csp = new RSACryptoServiceProvider();
        csp.ImportParameters(PublicKey);

        var chunks = data.Chunk(245).ToArray();
        var step = 1.0 / chunks.Length;
        var lastReportedProgress = 0.0;
        
        return chunks
            .Select((x, i) =>
            {
                var encrypted = csp.Encrypt(x, RSAEncryptionPadding.Pkcs1);
                
                var totalProgress = (i + 1) * step;
                if (totalProgress - lastReportedProgress >= UpdateThreshold)
                {
                    lastReportedProgress = totalProgress;
                    progress?.Report(totalProgress);
                }
                
                return encrypted;
            })
            .SelectMany(x => x)
            .ToArray();
    }

    public byte[] Decrypt(byte[] encryptedData, bool useLibrary, IProgress<double>? progress = null)
    {
        using var csp = new RSACryptoServiceProvider();
        csp.ImportParameters(PrivateKey);
        
        var chunks = encryptedData.Chunk(256)
            .Where(x => x.Length == 256)
            .ToArray();
        
        var step = 100.0 / chunks.Length;
        var lastReportedProgress = 0.0;
        
        return chunks
            .Select((x, i) =>
            {
                var decrypted = csp.Decrypt(x, RSAEncryptionPadding.Pkcs1);
                
                var totalProgress = (i + 1) * step;
                if (totalProgress - lastReportedProgress >= UpdateThreshold)
                {
                    lastReportedProgress = totalProgress;
                    progress?.Report(totalProgress);
                }
                
                return decrypted;
            })
            .SelectMany(x => x)
            .ToArray();
    }
}