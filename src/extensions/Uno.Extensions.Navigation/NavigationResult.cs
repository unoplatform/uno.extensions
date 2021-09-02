using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Navigation
{
    public record NavigationResult<TResult>(NavigationRequest Request, Task NavigationTask, Task<TResult> Response) : BaseNavigationResult(Request, NavigationTask) {
        public NavigationResult<TResult> GetAwaiter()
        {
            return this;
        }
    }


    public record NavigationResult(NavigationRequest Request, Task NavigationTask, Task<object> Response): BaseNavigationResult(Request, NavigationTask) {
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
                var task = Task.Delay(10000);
                task.GetAwaiter().OnCompleted(() =>
                {
                    IsCompleted = true;
                    continuation?.Invoke();
                });
            }
            else
            {
                IsCompleted = true;
            }
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
