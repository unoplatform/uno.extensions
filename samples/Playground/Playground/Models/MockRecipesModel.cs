using System.Collections.Immutable;
using Uno.Extensions.Reactive;

// Opt-in to generated mock factories (CreateMock + MockRecipesViewModelMocks) for this model only.
[assembly: Uno.Extensions.Reactive.Config.GenerateModelMocks("MockRecipesModel$")]

namespace Playground.Models;

public partial record Recipe(string Name, int Minutes);

/// <summary>
/// A plain MVUX model used by <see cref="Playground.Views.MvuxMocksPage"/> to demonstrate
/// pinning feed and command states with Uno.Extensions.Reactive.Mocks.
/// </summary>
public partial class MockRecipesModel
{
	public static IImmutableList<Recipe> SampleRecipes { get; } = ImmutableList.Create(
		new Recipe("Margherita pizza", 35),
		new Recipe("Lemon risotto", 45),
		new Recipe("Miso ramen", 60));

	public IListFeed<Recipe> Recipes => ListFeed.Async(LoadRecipes);

	// The "real" load takes a moment so the difference with a pinned Loading mock is visible.
	private async ValueTask<IImmutableList<Recipe>> LoadRecipes(CancellationToken ct)
	{
		await Task.Delay(TimeSpan.FromSeconds(1.5), ct);
		return SampleRecipes;
	}

	public async ValueTask Save(CancellationToken ct)
		=> await Task.Delay(TimeSpan.FromSeconds(2), ct);
}
