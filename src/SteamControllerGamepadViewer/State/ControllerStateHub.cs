using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace SteamControllerGamepadViewer.State;

public sealed class ControllerStateHub
{
    private readonly ConcurrentDictionary<Guid, Channel<ControllerSnapshot>> _subscribers = new();
    private readonly object _gate = new();
    private ControllerSnapshot _current = ControllerSnapshot.Starting();
    private long _version;

    public ControllerSnapshot Current
    {
        get
        {
            lock (_gate)
            {
                return _current;
            }
        }
    }

    public void Publish(ControllerSnapshot snapshot)
    {
        var next = snapshot with
        {
            Version = Interlocked.Increment(ref _version),
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        lock (_gate)
        {
            _current = next;
        }

        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Writer.TryWrite(next);
        }
    }

    public async IAsyncEnumerable<ControllerSnapshot> Subscribe([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<ControllerSnapshot>(new BoundedChannelOptions(4)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        _subscribers[id] = channel;
        channel.Writer.TryWrite(Current);

        try
        {
            while (await channel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (channel.Reader.TryRead(out var snapshot))
                {
                    yield return snapshot;
                }
            }
        }
        finally
        {
            _subscribers.TryRemove(id, out _);
            channel.Writer.TryComplete();
        }
    }
}
