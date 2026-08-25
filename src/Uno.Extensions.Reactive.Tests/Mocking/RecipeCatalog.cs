using System;
using Uno.Extensions.Reactive.Tests.MockingApp;
using Uno.HotTesting.Reactive;

namespace Uno.Extensions.Reactive.Tests.Mocking;

/// <summary>
/// Spec 013 tier 3 — sample of a hand-written named catalog: the one-line preview pattern. Each entry
/// builds the real <see cref="RecipeViewModel"/> (real model, null-injected service) pinned to a state,
/// via the generated <c>RecipeViewModelMock.Create(...)</c>. Access entries inside a
/// <see cref="MockingService.Enable"/> scope (e.g. a preview head or an assembly-init scope).
/// </summary>
public static class RecipeCatalog
{
	public static RecipeViewModel Loading => RecipeViewModelMock.Create(ListFeedMock.Loading<int>());

	public static RecipeViewModel Empty => RecipeViewModelMock.Create();

	public static RecipeViewModel Basic => RecipeViewModelMock.Create(ListFeedMock.Value(1, 2, 3));

	public static RecipeViewModel Failed => RecipeViewModelMock.Create(ListFeedMock.Error<int>(new TimeoutException()));
}
