using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationRequest(object Sender, Route Route, CancellationToken? Cancellation = default, Type Result = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    private RouteSegments segments;
    private string parsedRoute;

    // When we augment the route, the parsed segments also get copied. The check
    // against the parsedRoute will ensure the segments are recreated as required.
    public RouteSegments Segments => (parsedRoute == Route.Uri.OriginalString ? segments : null) ?? (segments = BuildSegments());

    private RouteSegments BuildSegments()
    {
        parsedRoute = Route.Uri.OriginalString;
        return this.Parse();
    }

    public override string ToString() => $"Navigation Request [Path:{Route?.Uri?.OriginalString}]";
}
