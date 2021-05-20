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
    public abstract record BaseRoutingMessage(object? Sender = null) { };

    public record LaunchMessage() : BaseRoutingMessage() { };

    public record ShowMessage(object? Sender = null) : BaseRoutingMessage(Sender) { };

    public record ShowItemMessage<TItem>(TItem ItemToShow, object? Sender = null) : BaseRoutingMessage(Sender) { };

    public record CloseMessage(bool? canBeCancelled = true, object? Sender = null) : BaseRoutingMessage(Sender) { };

    public record SelectedItemMessage<TItem>(TItem ItemSelected, object? Sender = null) : BaseRoutingMessage(Sender) { };

    public record SelectItemMessage<TItem>(IEnumerable<TItem> ItemsToSelectFrom, object? Sender = null) : BaseRoutingMessage(Sender) { };
}

