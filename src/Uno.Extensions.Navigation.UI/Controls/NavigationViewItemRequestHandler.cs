using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;

namespace Uno.Extensions.Navigation.UI;

/// <summary>
/// Navigation request handler for <see cref="NavigationViewItem"/>.
/// </summary>
/// <param name="HandlerLogger">Logger for logging</param>
/// <param name="Resolver">Resolver for navigation</param>
public sealed record NavigationViewItemRequestHandler(ILogger<NavigationViewItemRequestHandler> HandlerLogger, IRouteResolver Resolver) : ActionRequestHandlerBase<NavigationViewItem>(HandlerLogger, Resolver)
{
	/// <inheritdoc/>
	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var viewButton = view as NavigationViewItem;
		if (viewButton is null)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Bind: {view?.GetType()} is not a NavigationViewItem");
			}
			return default;
		}

		var parent = VisualTreeHelper.GetParent(view);
		while (parent is not null && parent is not NavigationView)
		{
			parent = VisualTreeHelper.GetParent(parent);
		}
		if (parent is null)
		{
			return default;
		}
		return BindAction((NavigationView)parent,
			action => new TypedEventHandler<NavigationView, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs>((sender, args) =>
			{
				if ((args.InvokedItemContainer is FrameworkElement navItem && navItem == viewButton))
				{
					action((FrameworkElement)args.InvokedItemContainer, default);
				}
			}),
			(element, handler) => element.ItemInvoked += handler,
			(element, handler) => element.ItemInvoked -= handler);
	}
}
