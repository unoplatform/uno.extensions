namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadCtorDepsModel(string Prefix)
{
	public IFeed<string> Value => Feed.Async(async ct => $"{Prefix}-original");
}
