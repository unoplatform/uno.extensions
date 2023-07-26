namespace Uno.Extensions.Maui.Interop;

internal static class MauiInterop
{
	private static readonly List<ControlMapping> _mappings = new();
	private static readonly List<IWinUIToMauiStyleHandler> _styleHandlers = new();

	static MauiInterop()
	{
		MapControl<TextBlock, Microsoft.Maui.Controls.Label>();
		MapControl<TextBox, Microsoft.Maui.Controls.Entry>();
		MapControl<Grid, Microsoft.Maui.Controls.Grid>();
		MapControl<StackPanel, Microsoft.Maui.Controls.StackLayout>();

		MapStyleHandler<LabelStyleHandler>();
		MapStyleHandler<VisualElementStyleHandler>();
		MapStyleHandler<ViewStyleHandler>();
		MapStyleHandler<LayoutStyleHandler>();
	}

	public static void MapControl<TWinUI, TMaui>()
		where TWinUI : FrameworkElement
		where TMaui : Microsoft.Maui.Controls.View =>
		_mappings.Add(new(typeof(TWinUI), typeof(TMaui)));

	public static void MapStyleHandler<THandler>()
		where THandler : IWinUIToMauiStyleHandler, new() =>
		_styleHandlers.Add(new THandler());

	public static bool TryGetMapping(Type winUI, out Type? maui)
	{
		maui = _mappings.FirstOrDefault(x => x.WinUI == winUI)?.Maui;
		return maui is not null;
	}

	public static bool TryGetStyle(Style winUIStyle, out Microsoft.Maui.Controls.Style? style)
	{
		style = null;

		if (winUIStyle.TargetType is null
			|| !TryGetMapping(winUIStyle.TargetType, out var targetType)
			|| targetType is null
			|| (!winUIStyle.Setters.Any() && winUIStyle.BasedOn is null))
		{
			return false;
		}

		var tempStyle = new Microsoft.Maui.Controls.Style(targetType);
		foreach (var setter in winUIStyle.Setters.OfType<Setter>())
		{
			if (setter.Property is null || setter.Value is null)
			{
				continue;
			}

			var handlers = GetHandlers(targetType);


			foreach (var handler in handlers)
			{
				var processed = handler.Process(setter.Property, setter.Value);
				if (processed is null)
				{
					continue;
				}

				tempStyle.Setters.Add(new Microsoft.Maui.Controls.Setter { Property = processed.Property, Value = processed.Value });
			}
		}

		style = tempStyle;

		return style.Setters.Any() || style.BasedOn is not null;
	}

	private static List<IWinUIToMauiStyleHandler> GetHandlers(Type targetType) =>
		_styleHandlers.Where(x => x.TargetType == targetType
			|| x.TargetType.IsAssignableFrom(targetType)).ToList();

	private record ControlMapping(Type WinUI, Type Maui);
}
