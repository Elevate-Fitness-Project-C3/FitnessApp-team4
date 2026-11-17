namespace IdentityService.Email;

public sealed class EmailOutboxOptions
{
    public int MaxDeliveryAttempts { get; init; } = 4;
    public int LeaseSeconds { get; init; } = 120;
    public int RenewalSeconds { get; init; } = 30;
    public int ProcessingTimeoutSeconds { get; init; } = 180;
}
