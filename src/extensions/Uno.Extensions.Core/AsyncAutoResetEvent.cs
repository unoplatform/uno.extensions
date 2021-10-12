using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions;

public class AsyncAutoResetEvent
{
    private readonly AutoResetEvent _event;

    public AsyncAutoResetEvent(bool initialState)
    {
        _event = new AutoResetEvent(initialState);
    }

    public Task<bool> Wait(TimeSpan? timeout = null)
    {
        return Task.Run(() =>
        {
            if (timeout.HasValue)
            {
                return _event.WaitOne(timeout.Value);
            }
            return _event.WaitOne();
        });
    }

    public void Set()
    {
        _event.Set();
    }
}
