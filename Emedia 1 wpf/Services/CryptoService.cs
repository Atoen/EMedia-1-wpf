using System.Security.Cryptography;

namespace Emedia_1_wpf.Services;

public class CryptoService
{
    private static readonly RSACryptoServiceProvider CryptoServiceProvider = new(2048);
    
    public byte[] Encrypt(byte[] data)
    {
        var parameters = CryptoServiceProvider.ExportParameters(true);
        using var rsa = RSA.Create(parameters);
        
        return data
            .Chunk(245)
            .Select(x => rsa.Encrypt(x, RSAEncryptionPadding.Pkcs1))
            .SelectMany(x => x)
            .ToArray();

    }

    public byte[] Decrypt(byte[] encryptedData)
    {
        var parameters = CryptoServiceProvider.ExportParameters(true);
        using var rsa = RSA.Create(parameters);

        return encryptedData
            .Chunk(256)
            .Select(x => rsa.Decrypt(x, RSAEncryptionPadding.Pkcs1))
            .SelectMany(x => x)
            .ToArray();
    }
}