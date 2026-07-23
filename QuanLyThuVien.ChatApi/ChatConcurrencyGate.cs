namespace QuanLyThuVien.ChatApi;

public sealed class ChatConcurrencyGate
{
    private readonly SemaphoreSlim _semaphore = new(2, 2);

    public async Task<IDisposable?> TryEnterAsync(CancellationToken cancellationToken)
    {
        if (!await _semaphore.WaitAsync(TimeSpan.Zero, cancellationToken)) return null;
        return new Lease(_semaphore);
    }

    private sealed class Lease(SemaphoreSlim semaphore) : IDisposable
    {
        private SemaphoreSlim? _semaphore = semaphore;

        public void Dispose() => Interlocked.Exchange(ref _semaphore, null)?.Release();
    }
}
