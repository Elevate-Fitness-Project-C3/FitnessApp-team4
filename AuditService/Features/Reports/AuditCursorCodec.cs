using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace AuditService.Features.Reports;

public sealed record AuditCursor(DateTimeOffset Time, long Id, string FilterHash);

public sealed class AuditCursorCodec(IConfiguration configuration)
{
    public bool IsConfigured => configuration["Reporting:CursorKey"]?.Length >= 32;
    public string Encode(AuditCursor cursor)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(cursor);
        return WebEncoders.Base64UrlEncode(bytes) + "." + WebEncoders.Base64UrlEncode(Sign(bytes));
    }
    public AuditCursor? Decode(string token, string filterHash)
    {
        if (token.Length > 1024) return null;
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 2) return null;
            var bytes = WebEncoders.Base64UrlDecode(parts[0]);
            if (!CryptographicOperations.FixedTimeEquals(Sign(bytes), WebEncoders.Base64UrlDecode(parts[1]))) return null;
            var cursor = JsonSerializer.Deserialize<AuditCursor>(bytes);
            return cursor?.FilterHash == filterHash && cursor.Id > 0 ? cursor : null;
        }
        catch (Exception exception) when (exception is FormatException or JsonException) { return null; }
    }
    public static string FilterHash(AuditEventSearchRequest request, AuditService.Security.AuditAccess access)
        => Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
        {
            Request = request with { Cursor = null }, access.UserId,
            Scopes = access.Scopes.OrderBy(x => x.ApplicationId, StringComparer.Ordinal).ThenBy(x => x.TenantId, StringComparer.Ordinal).ThenBy(x => x.SourceService, StringComparer.Ordinal)
        })));
    private byte[] Sign(byte[] bytes)
    {
        var key = configuration["Reporting:CursorKey"];
        if (key?.Length < 32 || key is null) throw new InvalidOperationException("Configure a strong reporting cursor key.");
        return HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), bytes);
    }
}
