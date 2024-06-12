using System.Collections;

namespace Uno.Extensions.Navigation.Navigators;

public class NavigationViewNavigator : SelectorNavigator<Microsoft.UI.Xaml.Controls.NavigationView>
{
	public NavigationViewNavigator(
		ILogger<NavigationViewNavigator> logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		RegionControlProvider controlProvider)
		: base(logger, dispatcher, region, resolver, controlProvider)
	{
	}

	public override void ControlInitialize()
	{
		// Make sure selectionchanged event handlers are wired up
		base.ControlInitialize();

		if(Control?.SelectedItem is not null)
		{
			_ = SelectionChanged(Control, MenuItemToFrameworkElement(Control.SelectedItem));
		}

	}

	protected override FrameworkElement? SelectedItem
	{
		get => Control is null ? null : MenuItemToFrameworkElement(Control.SelectedItem);
		set
		{
			if (Control is not null && value is not null)
			{
				Control.SelectedItem = FrameworkElementToMenuItem(value);
			}
		}
	}

	private FrameworkElement? MenuItemToFrameworkElement(object mi)
	{
		if (Control is null)
		{
			return null;
		}

		return mi is FrameworkElement fe ? fe : Control.ContainerFromMenuItem(mi) as FrameworkElement;
	}

	private object? FrameworkElementToMenuItem(FrameworkElement fe)
	{
		if (Control is null)
		{
			return null;
		}

		var item = (from mi in NavigationMenuItems
					let element = MenuItemToFrameworkElement(mi)
					where element == fe
					select element).FirstOrDefault();
		return item;
	}

	private object[] NavigationMenuItems
	{
		get
		{
			if (Control is null)
			{
				return Array.Empty<object>();
			}

			if (Control.MenuItemsSource is IEnumerable ||
				Control.FooterMenuItemsSource is IEnumerable)
			{
				var menuItemsSource = Control.MenuItemsSource is IEnumerable<NavigationViewItem> itemsSource ?
					itemsSource.ToArray() : Array.Empty<NavigationViewItem>();
				var footerMenuItemsSource = Control.FooterMenuItemsSource is IEnumerable<NavigationViewItem> footerItemsSource ?
					footerItemsSource.ToArray() : Array.Empty<NavigationViewItem>();

				var allitems = menuItemsSource.Concat(footerMenuItemsSource).ToArray();

				return allitems;
			}

			var menuItems = Control.MenuItems.OfType<NavigationViewItem>();
			var footerMenuItems = Control.FooterMenuItems.OfType<NavigationViewItem>();
			var navigationViewItems = menuItems.Concat(footerMenuItems).ToArray();

			return navigationViewItems;
		}
	}

	protected override IEnumerable<FrameworkElement> Items
	{
		get
		{
			if (Control is null)
			{
				return Array.Empty<FrameworkElement>();
			}

			var elements = (from mi in NavigationMenuItems
							let element = MenuItemToFrameworkElement(mi)
							where element is not null
							select element).ToArray();
			return elements;
		}
	}

	protected override Action? AttachSelectionChanged(Action<FrameworkElement, FrameworkElement?> selectionChanged)
	{
		var control = Control;
		if (control is null)
		{
			return default;
		}

		TypedEventHandler<Microsoft.UI.Xaml.Controls.NavigationView, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs> handler =
			(nv, args) =>
			{
				var selectedElement = MenuItemToFrameworkElement(args.SelectedItem);
				if (selectedElement is not null)
				{
					selectionChanged(nv, selectedElement);
				}
			};

		control.SelectionChanged += handler;
		return () => control.SelectionChanged -= handler;

	}
}
