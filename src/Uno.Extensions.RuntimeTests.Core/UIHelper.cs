using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;

namespace Uno.UI.RuntimeTests;

public static class UIHelper
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Action<UIElement?>? ContentSetter { get; set; }

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Func<UIElement?>? ContentGetter { get; set; }

	public static UIElement? Content
	{
		get => ContentGetter?.Invoke();
		set => ContentSetter?.Invoke(value);
	}
}
