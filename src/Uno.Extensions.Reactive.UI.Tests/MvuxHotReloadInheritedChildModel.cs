namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadInheritedChildModel : MvuxHotReloadParentModel
{
	public IFeed<string> ChildValue => Feed.Async(async ct => "child-original");
}
