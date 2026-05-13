namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadParentModel
{
	public IFeed<string> ParentValue => Feed.Async(async ct => "parent-original");
}
