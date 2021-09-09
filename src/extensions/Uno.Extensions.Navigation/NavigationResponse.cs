using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public record NavigationResponse<TResult>(NavigationRequest Request, Task NavigationTask, CancellationTokenSource CancellationSource, Task<TResult> Result) : BaseNavigationResponse(Request, NavigationTask)
    {
        public NavigationResponse<TResult> GetAwaiter()
        {
            return this;
        }
    }

    public record NavigationResponse(NavigationRequest Request, Task NavigationTask, CancellationTokenSource CancellationSource, Task<object> Result) : BaseNavigationResponse(Request, NavigationTask)
    {
        public NavigationResponse GetAwaiter()
        {
            return this;
        }
    }

    public record BaseNavigationResponse(NavigationRequest Request, Task NavigationTask) : INotifyCompletion
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
}
