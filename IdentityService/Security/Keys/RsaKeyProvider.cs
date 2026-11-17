using System.Security.Cryptography;
using IdentityService.Security.Options;

namespace IdentityService.Security.Keys;

public sealed class RsaKeyProvider : IDisposable
{
    private readonly RSA _privateKey;
    private readonly RSA _publicKey;
    public SigningCredentials SigningCredentials { get; }
    public RsaSecurityKey ValidationKey { get; }
    public RsaKeyProvider(IOptions<RsaKeyOptions> options)
    {
        _privateKey = RsaKeyLoader.LoadPrivateKey(options.Value);
        try
        {
            _publicKey = RsaKeyLoader.LoadPublicKey(options.Value);
            try
            {
                RsaKeyLoader.ValidateKeyPair(_privateKey, _publicKey);
                var keyId = Base64UrlEncoder.Encode(
                    SHA256.HashData(_publicKey.ExportSubjectPublicKeyInfo()));
                var signingKey = new RsaSecurityKey(_privateKey) { KeyId = keyId };
                ValidationKey = new RsaSecurityKey(_publicKey) { KeyId = keyId };
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);
            }
            catch
            {
                _publicKey.Dispose();
                throw;
            }
        }
        catch
        {
            _privateKey.Dispose();
            throw;
        }
    }
    public void Dispose()
    {
        _privateKey.Dispose();
        _publicKey.Dispose();
    }
}
