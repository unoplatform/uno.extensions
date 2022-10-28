namespace Uno.Extensions.Navigation;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
public record NavigationRequest(object Sender, Route Route, CancellationToken? Cancellation = default, Type? Result = null, INavigator? Source = null)
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter
{
	internal bool InProgress { get; init; }

	public override string ToString() => $"Request [Sender: {Sender.GetType().Name}, Route:{Route}, Result: {Result?.Name ?? "N/A"}]";

	internal virtual IResponseNavigator? GetResponseNavigator(IResponseNavigatorFactory responseFactory, INavigator navigator) => default;
}

public record NavigationRequest<TResult>(object Sender, Route Route, CancellationToken? Cancellation = default) : NavigationRequest(Sender, Route, Cancellation, typeof(TResult))
{
	internal override IResponseNavigator? GetResponseNavigator(IResponseNavigatorFactory responseFactory, INavigator navigator) => responseFactory.CreateForResultType<TResult>(navigator, this);
}
