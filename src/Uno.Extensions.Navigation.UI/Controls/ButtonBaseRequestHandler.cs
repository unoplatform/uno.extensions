namespace Uno.Extensions.Navigation.UI;

public class ButtonBaseRequestHandler : ActionRequestHandlerBase<ButtonBase>
{
	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var viewButton = view as ButtonBase;
		if (viewButton is null)
		{
			return null;
		}

		return BindAction(viewButton,
			action => new RoutedEventHandler((sender, args) => action((ButtonBase)sender)),
			(element, handler) => element.Click += handler,
			(element, handler) => element.Click -= handler);
	}
}
