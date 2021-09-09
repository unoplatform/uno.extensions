using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public record NavigationResult<TResult>(NavigationRequest Request, Task NavigationTask, CancellationTokenSource CancellationSource, Task<TResult> Result) : BaseNavigationResult(Request, NavigationTask)
    {
        public NavigationResult<TResult> GetAwaiter()
        {
            return this;
        }
    }

    public record NavigationResult(NavigationRequest Request, Task NavigationTask, CancellationTokenSource CancellationSource, Task<object> Result) : BaseNavigationResult(Request, NavigationTask)
    {
        public NavigationResult GetAwaiter()
        {
            return this;
        }
    }

    public record BaseNavigationResult(NavigationRequest Request, Task NavigationTask) : INotifyCompletion
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
