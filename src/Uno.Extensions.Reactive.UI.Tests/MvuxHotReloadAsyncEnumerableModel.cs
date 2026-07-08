using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive.WinUI.Tests;

public partial record MvuxHotReloadAsyncEnumerableModel
{
	public IFeed<string> CurrentValue => Feed.AsyncEnumerable(GetValues);

	private async IAsyncEnumerable<string> GetValues([EnumeratorCancellation] CancellationToken ct = default)
	{
		yield return "enumerable";
	}
}
