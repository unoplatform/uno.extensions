using System;

namespace Uno.Extensions.Navigation
{
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public record NavigationRequest(object Sender, NavigationRoute Route, Type Response = null)
    {
    }
#pragma warning enable SA1313 // Parameter names should begin with lower-case letter
}
