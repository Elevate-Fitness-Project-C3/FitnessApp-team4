using System.Security.Cryptography;
namespace IdentityService.Security.Keys;
internal static class DevelopmentRsaSetup
{
    public static void Ensure(IConfiguration configuration)
    {
        var privatePath = configuration["RsaKeys:PrivateKeyPath"];
        var publicPath = configuration["RsaKeys:PublicKeyPath"];
        if (string.IsNullOrWhiteSpace(privatePath) || string.IsNullOrWhiteSpace(publicPath))
            throw new InvalidOperationException("Development RSA paths are required.");
        privatePath = Path.GetFullPath(privatePath);
        publicPath = Path.GetFullPath(publicPath);
        if (privatePath == publicPath) throw new InvalidOperationException("RSA paths must be distinct.");
        Directory.CreateDirectory(Path.GetDirectoryName(privatePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(publicPath)!);
        using var gate = AcquireLock(privatePath + ".lock");
        if (!File.Exists(privatePath))
        {
            if (File.Exists(publicPath)) throw new InvalidOperationException("RSA private key is missing; restore the original key.");
            using var generated = RSA.Create(2048);
            WriteNew(privatePath, generated.ExportPkcs8PrivateKeyPem(), true);
        }
        using var rsa = RSA.Create();
        rsa.ImportFromPem(File.ReadAllText(privatePath));
        if (rsa.KeySize < 2048) throw new CryptographicException("RSA key is too small.");
        if (!File.Exists(publicPath)) WriteNew(publicPath, rsa.ExportSubjectPublicKeyInfoPem(), false);
    }
    private static FileStream AcquireLock(string path)
    {
        for (var attempt = 0; ; attempt++)
        {
            try { return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException) when (attempt < 30) { Thread.Sleep(1000); }
        }
    }
    private static void WriteNew(string path, string pem, bool secret)
    {
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
            if (!OperatingSystem.IsWindows() && secret) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            using (var stream = new FileStream(temp, options))
            using (var writer = new StreamWriter(stream)) writer.Write(pem);
            File.Move(temp, path);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
