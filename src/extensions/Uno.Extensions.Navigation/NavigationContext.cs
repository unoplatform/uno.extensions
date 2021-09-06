using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public record NavigationContext(
        IServiceProvider Services,
        NavigationRequest Request,
        string Path,
        bool PathIsRooted,
        int FramesToRemove,
        IDictionary<string,object> Data,
        CancellationTokenSource CancellationSource,
        TaskCompletionSource<object> ResponseCompletion,
        bool CanCancel,
        NavigationMap Mapping = null)
    {
        public CancellationToken CancellationToken => CancellationSource.Token;

        public void Cancel()
        {
            if (CanCancel)
            {
                CancellationSource.Cancel();
            }
        }
    }
}
