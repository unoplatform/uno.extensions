using Microsoft.Maui.Controls;
using ContentPropertyAttribute = Microsoft.UI.Xaml.Markup.ContentPropertyAttribute;

namespace Uno.Extensions.Maui;

[ContentProperty(Name = nameof(Key))]
[MarkupExtensionReturnType(ReturnType = typeof(object))]
public class MauiResource : MauiExtensionBase
{
	public string Key { get; set; } = string.Empty;

	protected override void SetValue(View view, Type viewType, Type propertyType, BindableProperty property, string propertyName)
	{
		if (string.IsNullOrEmpty(Key))
		{
			return;
		}
		else if (view.Resources.ContainsKey(Key) && view.Resources.TryGetValue(Key, out var value) && propertyType.IsAssignableFrom(value.GetType()))
		{
			view.SetValue(property, value);
		}
		else
		{
			view.SetDynamicResource(property, Key);
		}
	}
}
