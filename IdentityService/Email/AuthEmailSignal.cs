namespace IdentityService.Email;

public sealed class AuthEmailSignal
{
    private readonly SemaphoreSlim _semaphore = new(0, int.MaxValue);

    public void Notify() => _semaphore.Release();

    public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!await _semaphore.WaitAsync(timeout, cancellationToken))
            return;
        while (_semaphore.CurrentCount > 0 && await _semaphore.WaitAsync(0, cancellationToken)) { }
    }
}
