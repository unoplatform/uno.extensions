using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;

namespace Uno.Extensions.Navigation.UI;

public class NavigationViewItemRequestHandler : ActionRequestHandlerBase<NavigationViewItem>
{
	public NavigationViewItemRequestHandler(IRouteResolver routes) : base(routes)
	{
		DefaultQualifier = Qualifiers.ChangeContent;
	}

	public override IRequestBinding? Bind(FrameworkElement view)
	{
		var viewButton = view as NavigationViewItem;
		if (viewButton is null)
		{
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
					action((FrameworkElement)args.InvokedItemContainer);
				}
			}),
			(element, handler) => element.ItemInvoked += handler,
			(element, handler) => element.ItemInvoked -= handler);
	}
}
