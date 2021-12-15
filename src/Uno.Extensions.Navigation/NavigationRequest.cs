namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationRequest(object Sender, Route Route, CancellationToken? Cancellation = default, Type? Result = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
    public override string ToString() => $"Request [Sender: {Sender.GetType().Name}, Route:{Route}, Result: {Result?.Name ?? "N/A"}]";

	internal virtual IResponseNavigator? GetResponseNavigator(IResponseNavigatorFactory responseFactory, INavigator navigator)=> default;

	//// This is used to convert a NavigationRequest<TResult> back to a NavigationRequest (ie with no Result type)
	//// request = request with {Result = null)  this doesn't work as you still get a NavigationRequest<TResult>, just with Result set to null instead of typeof(TResult)
	//internal NavigationRequest NoResult() => new NavigationRequest(Sender, Route, Cancellation, null);
}

public record NavigationRequest<TResult>(object Sender, Route Route, CancellationToken? Cancellation = default) : NavigationRequest(Sender, Route, Cancellation, typeof(TResult))
{
    internal override IResponseNavigator? GetResponseNavigator(IResponseNavigatorFactory responseFactory, INavigator navigator)=> responseFactory.CreateForResultType<TResult>(navigator, this);
}
