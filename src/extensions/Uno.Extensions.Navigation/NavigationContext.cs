using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationContext(
    IServiceProvider Services,
    NavigationRequest Request,
    string Path,
    bool PathIsRooted,
    int FramesToRemove,
    IDictionary<string, object> Data,
    CancellationTokenSource CancellationSource,
    TaskCompletionSource<Options.Option> ResultCompletion,
    bool CanCancel = true,
    NavigationMap Mapping = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{

    public bool IsBackNavigation => Path == NavigationConstants.PreviousViewUri;

    public CancellationToken CancellationToken => CancellationSource.Token;

    public void Cancel()
    {
        if (CanCancel)
        {
            CancellationSource.Cancel();
        }
    }

    public bool IsCancelled => CancellationToken.IsCancellationRequested || (Request.Cancellation?.IsCancellationRequested ?? false);
}
