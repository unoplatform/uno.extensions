using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

public class NavigationResponse<TResult> : BaseNavigationResponse
{
    public Task<Options.Option<TResult>> Result { get; }

    public NavigationResponse(NavigationRequest request, Task navigationTask, Task<Options.Option<TResult>> result) : base(request, navigationTask)
    {
        Result = result;
    }

    public NavigationResponse(NavigationResponse response) : base(response.Request, response.NavigationTask)
    {
        Result = response.Result.ContinueWith(x =>
                    (x.Result.MatchSome(out var val) && val is TResult tval) ?
                        Options.Option.Some(tval) :
                        Options.Option.None<TResult>());
    }

    public NavigationResponse<TResult> GetAwaiter()
    {
        return this;
    }
}

public class NavigationResponse : BaseNavigationResponse
{
    public Task<Options.Option> Result { get; }

    public NavigationResponse(NavigationRequest request, Task navigationTask, Task<Options.Option> result) : base(request, navigationTask)
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

    public async void OnCompleted(Action continuation)
    {
        if (NavigationTask is not null)
        {
            await NavigationTask;
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
