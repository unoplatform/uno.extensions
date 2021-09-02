using System;
using System.Threading;

namespace Uno.Extensions.Navigation
{
    public interface INavigationContext
    {
        IServiceProvider Services { get; }

        NavigationRequest Request { get; }

        NavigationMap Mapping { get; }

        CancellationToken CancellationToken { get; }

        bool CanCancel { get; }

        void Cancel();
    }
}
