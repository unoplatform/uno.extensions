using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Navigation;
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationContext(
    IServiceProvider Services,
    NavigationRequest Request,
    CancellationTokenSource CancellationSource,
    RouteMap Mapping,
    bool CanCancel = true)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    public INavigationService Navigation => Services.GetService<INavigationService>();

    public bool IsBackNavigation => Request.Route.FrameIsBackNavigation;

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
