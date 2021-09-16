using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public class NavigationResponse<TResult> : BaseNavigationResponse
{
    public Task<TResult> Result { get; }

    public NavigationResponse(NavigationRequest request, Task navigationTask, Task<TResult> result) : base(request, navigationTask)
    {
        Result = result;
    }

    public NavigationResponse(NavigationResponse response) : base(response.Request, response.NavigationTask)
    {
        Result = response.Result.ContinueWith(x => (TResult)x.Result);
    }

    public NavigationResponse<TResult> GetAwaiter()
    {
        return this;
    }
}

public class NavigationResponse : BaseNavigationResponse
{
    public Task<object> Result { get; }

    public NavigationResponse(NavigationRequest request, Task navigationTask, Task<object> result) : base(request, navigationTask)
    {
        Result = result;
    }

    public NavigationResponse GetAwaiter()
    {
        return this;
    }
}

public class BaseNavigationResponse : INotifyCompletion
{
    public NavigationRequest Request { get; }

    public Task NavigationTask { get; }

    protected BaseNavigationResponse(NavigationRequest request, Task navigationTask)
    {
        Request = request;
        NavigationTask = navigationTask;
    }

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
