using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.Extensions.Reactive;

public partial class ViewDebugger : DependencyObject
{
	private static readonly ConditionalWeakTable<UIElement, ViewDebugger> _debuggers = new();

	public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
		"IsEnabled", typeof(bool), typeof(ViewDebugger), new PropertyMetadata(default(bool), (snd, e) => GetDebugger(snd)?.Enable((bool)e.NewValue)));

	public static bool GetIsEnabled(UIElement element)
		=> (bool)element.GetValue(IsEnabledProperty);

	public static void SetIsEnabled(UIElement element, bool isEnabled)
		=> element.SetValue(IsEnabledProperty, isEnabled);

	private readonly VisualStateTracker? _visualStates;

	public ViewDebugger(UIElement element)
	{
		if (element is Control ctrl)
		{
			_visualStates = new(ctrl);
		}
	}

	private static ViewDebugger? GetDebugger(DependencyObject owner)
		=> owner is UIElement elt
			? _debuggers.GetValue(elt, e => new ViewDebugger(e))
			: default;

	private void Enable(bool isEnabled)
	{
		if (isEnabled)
		{
			_visualStates?.Enable();
		}
		else
		{
			_visualStates?.Disable();
		}
	}
}
