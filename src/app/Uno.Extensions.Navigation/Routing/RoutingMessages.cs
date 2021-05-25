//-:cnd:noEmit
#if WINDOWS_UWP
//+:cnd:noEmit
//-:cnd:noEmit
#else
//+:cnd:noEmit
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
//-:cnd:noEmit
#endif
//+:cnd:noEmit

using System.Collections.Generic;

namespace Uno.Extensions.Navigation
{
    public record RoutingMessage(object? Sender = null, string path = "", IDictionary<string, object> args = null) { };

    public record LaunchMessage() : RoutingMessage() { };

    public record ClearStackMessage(object? Sender = null, string path="") : RoutingMessage(Sender, path: $"/{path}") { };

    public record ActionMessage<TAction>(
        object? Sender = null,
        string actionArgumentKey = default,
        TAction action = default) :
        RoutingMessage(Sender, args: new Dictionary<string,object>() { { actionArgumentKey, action } });

    //public record ShowMessage(object? Sender = null) : BaseRoutingMessage(Sender) { };

    //public record ShowItemMessage<TItem>(TItem ItemToShow, object? Sender = null) : BaseRoutingMessage(Sender) { };

    public record CloseMessage(object? Sender = null) : RoutingMessage(Sender, path: "..") { };

    //public record SelectedItemMessage<TItem>(TItem ItemSelected, object? Sender = null) : BaseRoutingMessage(Sender) { };

    //public record SelectItemMessage<TItem>(IEnumerable<TItem> ItemsToSelectFrom, object? Sender = null) : BaseRoutingMessage(Sender) { };
}

