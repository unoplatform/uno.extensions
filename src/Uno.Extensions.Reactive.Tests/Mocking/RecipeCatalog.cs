using Uno.Extensions.Reactive.Tests.MockingApp;
using Uno.HotTesting.Reactive;

namespace Uno.Extensions.Reactive.Tests.MockingApp;

/// <summary>
/// Spec 013 tier 3 — sample of named catalog entries added as a <b>partial</b> of the generated
/// <see cref="RecipeViewModelMock"/> factory. Each entry builds the real view-model pinned to a state
/// via <c>Create(...)</c> (which opens the activation scope internally). Bind a page to one entry for a
/// one-line preview: <c>DataContext="{x:Bind RecipeViewModelMock.Basic}"</c>.
/// </summary>
public static partial class RecipeViewModelMock
{
	public static RecipeViewModel Loading => Create(RecipeModelMock.Empty with { Steps = ListFeedMock.Loading<int>() });

	public static RecipeViewModel Basic => Create(new RecipeModelMock { Steps = ListFeedMock.Value(1, 2, 3) });

	public static RecipeViewModel Failed => Create(RecipeModelMock.Empty with { Steps = ListFeedMock.Error<int>(new global::System.TimeoutException()) });
}
