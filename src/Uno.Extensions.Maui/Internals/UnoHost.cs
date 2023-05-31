namespace Uno.Extensions.Maui.Internals;

internal class UnoHost : Microsoft.Maui.Controls.VisualElement
{
	public UnoHost(ResourceDictionary resources)
	{
		Resources = resources.ToMauiResources();
	}
}
