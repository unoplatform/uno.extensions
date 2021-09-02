using System;
using System.Threading;

namespace Uno.Extensions.Navigation
{
    public record NavigationContext(
        IServiceProvider Services,
        NavigationRequest Request,
        CancellationTokenSource CancellationSource,
        bool CanCancel,
        NavigationMap Mapping = null): INavigationContext
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
