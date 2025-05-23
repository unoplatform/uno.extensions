namespace Uno.Extensions.Maui;

/// <summary>
/// A helper class used to set the value of a view's property by its key.
/// </summary>
[ContentProperty(Name = nameof(Key))]
[MarkupExtensionReturnType(ReturnType = typeof(object))]
public class MauiResource : MauiExtensionBase
{
	/// <summary>
	/// The key for the resource to be retrieved and set.
	/// </summary>
	public string Key { get; set; } = string.Empty;

	/// <summary>
	/// Sets the value of the view's property by the key.
	/// </summary>
	/// <param name="view">The <see cref="View"/> whose property value will be set.</param>
	/// <param name="viewType">The type of the view.</param>
	/// <param name="propertyType">The type of the property.</param>
	/// <param name="property">The property whose value will be set.</param>
	/// <param name="propertyName">The name of the property.</param>
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
