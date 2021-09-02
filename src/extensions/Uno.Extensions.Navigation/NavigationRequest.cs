using System;

namespace Uno.Extensions.Navigation
{
    public record NavigationRequest(object Sender, NavigationRoute Route, Type Response = null)
    {
    }
}
