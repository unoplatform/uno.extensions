using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions;

public class AsyncAutoResetEvent : IDisposable
{
    private readonly AutoResetEvent _event;
    private bool isDisposed;

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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (isDisposed)
        {
            return;
        }

        if (disposing)
        {
            // free managed resources
            _event?.Dispose();
        }

        isDisposed = true;
    }
}
