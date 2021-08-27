using System;

namespace Uno.Extensions.Navigation
{
    public record NavigationRoute(Uri Path, object Data = null)
    {
    }
}
