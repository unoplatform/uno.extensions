using System;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;

namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationRequest(object Sender, Route Route, CancellationToken? Cancellation = default, Type? Result = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    public override string ToString() => $"Request [Sender: {Sender.GetType().Name}, Route:{Route}, Result: {Result?.Name ?? "N/A"}]";

	internal virtual IResponseNavigator? GetResponseNavigator(INavigator navigator)=> default;
}

public record NavigationRequest<TResult>(object Sender, Route Route, CancellationToken? Cancellation = default) : NavigationRequest(Sender, Route, Cancellation, typeof(TResult))
{
    internal override IResponseNavigator? GetResponseNavigator(INavigator navigator)=> new ResponseNavigator<TResult>(navigator, this);
}
