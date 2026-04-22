namespace Uno.Extensions.Navigation.UI.Tests.ViewModels;

public sealed class HotReloadVm
{
	public string DisplayedValue => GetDisplayedValue();

	internal string GetDisplayedValue()
	{
		return "original";
	}
}
