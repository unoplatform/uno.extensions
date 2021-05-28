using System.Collections.Generic;

namespace Uno.Extensions.Navigation
{
#pragma warning disable SA1313, CA1801 // Parameter names should begin with lower-case letter - Exception for records
    public record RoutingMessage(object Sender = null, string Path = "", IDictionary<string, object> Args = null) { };
#pragma warning restore SA1313, CA1801 // Parameter names should begin with lower-case letter

    public record LaunchMessage() : RoutingMessage() { };

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter - Exception for records
    public record ClearStackMessage(object Sender = null, string Path = "") : RoutingMessage(Sender, Path: $"/{Path}") { };
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter - Exception for records
    public record ActionMessage<TAction>(
        object Sender = null,
        string ActionArgumentKey = default,
        TAction Action = default) :
        RoutingMessage(Sender, Args: new Dictionary<string, object>() { { ActionArgumentKey, Action } });
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter - Exception for records
    public record CloseMessage(object Sender = null) : RoutingMessage(Sender, Path: "..") { };
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
}
