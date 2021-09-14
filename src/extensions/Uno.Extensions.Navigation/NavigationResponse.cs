using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResponse<TResult>(NavigationRequest Request, Task NavigationTask, CancellationTokenSource CancellationSource, Task<TResult> Result) : BaseNavigationResponse(Request, NavigationTask)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    public NavigationResponse<TResult> GetAwaiter()
    {
        return this;
    }
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationResponse(NavigationRequest Request, Task NavigationTask, CancellationTokenSource CancellationSource, Task<object> Result) : BaseNavigationResponse(Request, NavigationTask)
{
    public NavigationResponse GetAwaiter()
    {
        return this;
    }
}

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record BaseNavigationResponse(NavigationRequest Request, Task NavigationTask) : INotifyCompletion
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    public void OnCompleted(Action continuation)
    {
        if (NavigationTask is not null)
        {
            continuation?.Invoke();
        }
        IsCompleted = true;
    }

    public bool IsCompleted
    {
        get;
        private set;
    }

    public void GetResult()
    {
    }
}
