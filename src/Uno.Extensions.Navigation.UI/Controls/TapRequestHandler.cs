namespace Uno.Extensions.Navigation.UI;

public class TapRequestHandler : ActionRequestHandlerBase<FrameworkElement>
{
	public TapRequestHandler(IRouteResolver routes) : base(routes)
	{
	}

	public override IRequestBinding? Bind(FrameworkElement view)
	{
		if (view is null)
		{
			return null;
		}

		return BindAction(view,
			action => new TappedEventHandler((sender, args) => action((FrameworkElement)sender)),
			(element, handler) => element.Tapped += handler,
			(element, handler) => element.Tapped -= handler);
	}
}
