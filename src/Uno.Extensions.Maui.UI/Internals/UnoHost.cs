namespace Uno.Extensions.Maui.Internals;

internal class UnoHost : Microsoft.Maui.Controls.VisualElement
{
	public UnoHost(ResourceDictionary resources)
	{
		Resources = resources.ToMauiResources();
	}

	protected override void OnBindingContextChanged()
	{

		if (BindingContext is null)
		{
			System.Console.WriteLine("UnoHost.BindingContext is null");
		}
		else
		{
			System.Console.WriteLine($"UnoHost.BindingContext is {BindingContext.GetType().FullName}");
		}

	}
}
