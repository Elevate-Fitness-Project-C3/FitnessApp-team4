namespace IdentityService.Email;
public sealed class EmailOptions
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public bool EnableSsl { get; init; } = true;
    public string UserName { get; init; } = "";
    public string Password { get; init; } = "";
    public string From { get; init; } = "";
}
