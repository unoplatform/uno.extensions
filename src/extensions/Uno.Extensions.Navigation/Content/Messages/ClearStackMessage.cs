namespace Uno.Extensions.Navigation.Messages
{
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter - Exception for records
    public record ClearStackMessage(object Sender = null, string Path = "") : RoutingMessage(Sender, Path: $"/{Path}") { };
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
}
