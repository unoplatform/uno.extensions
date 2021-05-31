using System.Collections.Generic;

namespace Uno.Extensions.Navigation
{
#pragma warning disable SA1313, CA1801 // Parameter names should begin with lower-case letter - Exception for records
    public record RoutingMessage(object Sender = null, string Path = "", IDictionary<string, object> Args = null) { };
#pragma warning restore SA1313, CA1801 // Parameter names should begin with lower-case letter
}
