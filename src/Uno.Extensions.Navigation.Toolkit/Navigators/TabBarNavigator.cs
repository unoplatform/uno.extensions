namespace Uno.Extensions.Navigation.Toolkit.Navigators;

public class TabBarNavigator : SelectorNavigator<TabBar>
{
	public TabBarNavigator(
		ILogger<TabBarNavigator> logger,
		IDispatcher dispatcher,
		IRegion region,
		IRouteResolver resolver,
		RegionControlProvider controlProvider)
		: base(logger, dispatcher, region, resolver, controlProvider)
	{
	}

	protected override FrameworkElement? SelectedItem
	{
		get => Control?.SelectedItem as FrameworkElement;
		set
		{
			if (Control is not null)
			{
				Control.SelectedItem = value!;
			}
		}
	}

	protected override IEnumerable<FrameworkElement> Items
	{
		get
		{
			if (Logger.IsEnabled(LogLevel.Trace))
			{
				Logger.LogTraceMessage($"{nameof(Items)}: {Control?.Items.Count}");
			}
			return Control?.Items.OfType<FrameworkElement>() ?? new FrameworkElement[] { };
		}
	}

	protected override Action? AttachSelectionChanged(Action<FrameworkElement, FrameworkElement?> selectionChanged)
	{
		var control = Control;
		if (control is null)
		{
			if (Logger.IsEnabled(LogLevel.Warning))
			{
				Logger.LogWarningMessage($"Unable to attach selection changed handler as Control is null");
			}
			return default;
		}

		TypedEventHandler<TabBar, TabBarSelectionChangedEventArgs> handler =
			(nv, args) =>
			{
				if (Logger.IsEnabled(LogLevel.Trace))
				{
					Logger.LogTraceMessage($"Tab bar selection changed ({(args.NewItem is not null ? "NewItem not null" : "NewItem is null")})");
				}
				selectionChanged(nv, args.NewItem as FrameworkElement);
			};

		control.SelectionChanged += handler;
		return () => control.SelectionChanged -= handler;
	}
}
