using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationRequest(object Sender, NavigationRoute Route, CancellationToken? Cancellation = default, Type Result = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    public string FirstRouteSegment
    {
        get
        {
            var path = Route.Uri.OriginalString;
            var idx = path.IndexOf('/');
            if (idx < 0)
            {
                return path;
            }

            return path.Substring(0, idx);
        }
    }

    public override string ToString() => $"Navigation Request [Path:{Route?.Uri?.OriginalString}]";
}
