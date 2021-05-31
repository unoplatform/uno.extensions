using System.Collections.Generic;

namespace Uno.Extensions.Navigation
{
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter - Exception for records
    public record ActionMessage<TAction>(
        object Sender = null,
        string ActionArgumentKey = default,
        TAction Action = default) :
        RoutingMessage(Sender, Args: new Dictionary<string, object>() { { ActionArgumentKey, Action } });
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
}
