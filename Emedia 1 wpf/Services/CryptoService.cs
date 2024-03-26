using System.Security.Cryptography;

namespace Emedia_1_wpf.Services;

public class CryptoService
{
    private static readonly RSACryptoServiceProvider CryptoServiceProvider = new(2048);
    private static readonly RSAParameters PrivateKey = CryptoServiceProvider.ExportParameters(true);
    private static readonly RSAParameters PublicKey = CryptoServiceProvider.ExportParameters(false);
    
    public byte[] Encrypt(byte[] data, bool useLibrary)
    {
        using var csp = new RSACryptoServiceProvider();
        csp.ImportParameters(PublicKey);
        
        return data
            .Chunk(245)
            .Select(x => csp.Encrypt(x, RSAEncryptionPadding.Pkcs1))
            .SelectMany(x => x)
            .ToArray();
    }

    public byte[] Decrypt(byte[] encryptedData, bool useLibrary)
    {
        using var csp = new RSACryptoServiceProvider();
        csp.ImportParameters(PrivateKey);
        
        return encryptedData
            .Chunk(256)
            .Where(x => x.Length == 256)
            .Select(x => csp.Decrypt(x, RSAEncryptionPadding.Pkcs1))
            .SelectMany(x => x)
            .ToArray();
    }
}