using System.Security.Cryptography;
using IdentityService.Security.Options;

namespace IdentityService.Security.Keys;

public static class RsaKeyLoader
{
    private const int MinimumKeySize = 2048;
    public static RSA LoadPrivateKey(RsaKeyOptions options)
    {
        var rsa = LoadKey(options.PrivateKeyPath);
        try
        {
            var parameters = rsa.ExportParameters(true);
            try
            {
                if (parameters.D is null)
                    throw new CryptographicException("RSA private key is required.");
            }
            finally
            {
                ClearPrivateParameters(parameters);
            }
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }
    public static RSA LoadPublicKey(RsaKeyOptions options)
    {
        using var rsa = LoadKey(options.PublicKeyPath);
        var publicKey = RSA.Create();
        try
        {
            publicKey.ImportParameters(rsa.ExportParameters(false));
            return publicKey;
        }
        catch
        {
            publicKey.Dispose();
            throw;
        }
    }
    public static void ValidateKeyPair(RSA privateKey, RSA publicKey)
    {
        var privateKeyPublicPart = privateKey.ExportSubjectPublicKeyInfo();
        var publicKeyBytes = publicKey.ExportSubjectPublicKeyInfo();
        if (!privateKeyPublicPart.AsSpan().SequenceEqual(publicKeyBytes))
            throw new CryptographicException("RSA private and public keys do not match.");
    }
    private static RSA LoadKey(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("RSA key file was not found.", path);
        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(File.ReadAllText(path));
            if (rsa.KeySize < MinimumKeySize)
                throw new CryptographicException($"RSA key size must be at least {MinimumKeySize} bits.");
            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }
    private static void ClearPrivateParameters(RSAParameters parameters)
    {
        CryptographicOperations.ZeroMemory(parameters.D);
        CryptographicOperations.ZeroMemory(parameters.P);
        CryptographicOperations.ZeroMemory(parameters.Q);
        CryptographicOperations.ZeroMemory(parameters.DP);
        CryptographicOperations.ZeroMemory(parameters.DQ);
        CryptographicOperations.ZeroMemory(parameters.InverseQ);
    }
}