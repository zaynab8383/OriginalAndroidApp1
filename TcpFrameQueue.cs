namespace AndroidApp1;

internal sealed class TcpFrameQueue
{
    private const int MaxFrames = 400;

    private readonly object _gate = new();
    private readonly LinkedList<(byte[] Data, string? Key)> _items = new();
    private readonly SemaphoreSlim _available = new(0);

    public void Enqueue(byte[] data, string? coalesceKey = null)
    {
        lock (_gate)
        {
            var last = _items.Last;
            if (coalesceKey != null && last != null && last.Value.Key == coalesceKey)
            {
                last.Value = (data, coalesceKey);
                return;
            }

            _items.AddLast((data, coalesceKey));

            if (_items.Count > MaxFrames)
            {
                _items.RemoveFirst();
                return;
            }
        }

        _available.Release();
    }

    public async Task<byte[]> DequeueAsync(CancellationToken token)
    {
        while (true)
        {
            await _available.WaitAsync(token);

            lock (_gate)
            {
                var first = _items.First;
                if (first == null) continue;
                _items.RemoveFirst();
                return first.Value.Data;
            }
        }
    }

    public void Clear()
    {
        lock (_gate) _items.Clear();
    }
}