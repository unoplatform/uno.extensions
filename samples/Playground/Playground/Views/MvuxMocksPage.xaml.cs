using System.Collections.Immutable;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Mocks;

namespace Playground.Views;

public sealed partial class MvuxMocksPage : Page
{
	public MvuxMocksPage()
	{
		this.InitializeComponent();

		// Initial states so both sections show something right away.
		ShowDirectValue();
		ShowVmValue();
	}

	#region Section 1 — mock feeds bound directly to a FeedView
	private void OnDirectUndefined(object sender, RoutedEventArgs e)
		=> ShowDirect(
			MockListFeed.Undefined<Recipe>(),
			"""
			// using Uno.Extensions.Reactive.Mocks;
			DirectFeedView.Source = MockListFeed.Undefined<Recipe>();
			""");

	private void OnDirectLoading(object sender, RoutedEventArgs e)
		=> ShowDirect(
			MockListFeed.Loading<Recipe>(),
			"""
			// Loading, indefinitely — no never-completing Task, nothing leaks.
			DirectFeedView.Source = MockListFeed.Loading<Recipe>();
			""");

	private void OnDirectEmpty(object sender, RoutedEventArgs e)
		=> ShowDirect(
			MockListFeed.Empty<Recipe>(),
			"""
			// The "no results" state (Option.None).
			DirectFeedView.Source = MockListFeed.Empty<Recipe>();
			""");

	private void OnDirectValue(object sender, RoutedEventArgs e)
		=> ShowDirectValue();

	private void ShowDirectValue()
		=> ShowDirect(
			MockListFeed.Value(MockRecipesModel.SampleRecipes),
			"""
			DirectFeedView.Source = MockListFeed.Value(
			    new Recipe("Margherita pizza", 35),
			    new Recipe("Lemon risotto", 45),
			    new Recipe("Miso ramen", 60));
			""");

	private void OnDirectError(object sender, RoutedEventArgs e)
		=> ShowDirect(
			MockListFeed.Error<Recipe>(new TimeoutException("The recipe service timed out.")),
			"""
			DirectFeedView.Source = MockListFeed.Error<Recipe>(
			    new TimeoutException("The recipe service timed out."));
			""");

	private void OnDirectRefreshing(object sender, RoutedEventArgs e)
		=> ShowDirect(
			MockListFeed.Refreshing(MockRecipesModel.SampleRecipes.ToArray()),
			"""
			// Data present + progress, indefinitely. The default FeedView template shows its
			// progress overlay; the stale items remain available through State.Data.
			DirectFeedView.Source = MockListFeed.Refreshing(sampleRecipes);
			""");

	private void OnDirectScript(object sender, RoutedEventArgs e)
		=> ShowDirect(
			MockFeed.Script<IImmutableList<Recipe>>(
				(TimeSpan.Zero, m => m.IsTransient(true)),
				(TimeSpan.FromSeconds(2), m => m.Data(MockRecipesModel.SampleRecipes).IsTransient(false)),
				(TimeSpan.FromSeconds(2), m => m.Error(new TimeoutException("The refresh timed out.")))),
			"""
			// A scripted walk: loading (2s) -> data (2s) -> error.
			DirectFeedView.Source = MockFeed.Script<IImmutableList<Recipe>>(
			    (TimeSpan.Zero, m => m.IsTransient(true)),
			    (TimeSpan.FromSeconds(2), m => m.Data(sampleRecipes).IsTransient(false)),
			    (TimeSpan.FromSeconds(2), m => m.Error(new TimeoutException("The refresh timed out."))));
			""");

	private void ShowDirect(object source, string code)
	{
		// Re-inflate the template so a fresh FeedView renders each state — a reused FeedView keeps
		// visual-state residue (e.g. a pinned loading indicator) across source changes.
		DirectHost.ContentTemplate = null;
		DirectHost.Content = source;
		DirectHost.ContentTemplate = (DataTemplate)Resources["DirectFeedViewTemplate"];
		DirectCode.Text = code;
	}
	#endregion

	#region Section 2 — CreateMock on the generated ViewModel
	private void OnVmReal(object sender, RoutedEventArgs e)
		=> ShowVm(
			new MockRecipesViewModel(),
			"""
			// The real thing, for contrast: constructs MockRecipesModel and
			// actually loads (1.5s) — Save really runs for 2s.
			VmHost.DataContext = new MockRecipesViewModel();
			""");

	private void OnVmLoading(object sender, RoutedEventArgs e)
		=> ShowVm(
			MockRecipesViewModel.CreateMock(m => m.Recipes = MockListFeed.Loading<Recipe>()),
			"""
			// The model is never constructed; unconfigured members default to
			// Undefined and commands to an idle no-op.
			VmHost.DataContext = MockRecipesViewModel.CreateMock(m =>
			    m.Recipes = MockListFeed.Loading<Recipe>());
			""");

	private void OnVmEmpty(object sender, RoutedEventArgs e)
		=> ShowVm(
			MockRecipesViewModel.CreateMock(m =>
			{
				m.Recipes = MockListFeed.Empty<Recipe>();
				m.Save = MockCommand.Disabled();
			}),
			"""
			VmHost.DataContext = MockRecipesViewModel.CreateMock(m =>
			{
			    m.Recipes = MockListFeed.Empty<Recipe>();
			    m.Save    = MockCommand.Disabled();  // nothing to save
			});
			""");

	private void OnVmValue(object sender, RoutedEventArgs e)
		=> ShowVmValue();

	private void ShowVmValue()
		=> ShowVm(
			MockRecipesViewModel.CreateMock(m => m.Recipes = MockListFeed.Value(MockRecipesModel.SampleRecipes)),
			"""
			// Save is left unconfigured: it defaults to an enabled no-op.
			VmHost.DataContext = MockRecipesViewModel.CreateMock(m =>
			    m.Recipes = MockListFeed.Value(sampleRecipes));
			""");

	private void OnVmError(object sender, RoutedEventArgs e)
		=> ShowVm(
			MockRecipesViewModel.CreateMock(m => m.Recipes = MockListFeed.Error<Recipe>(new HttpRequestException("503 Service Unavailable"))),
			"""
			VmHost.DataContext = MockRecipesViewModel.CreateMock(m =>
			    m.Recipes = MockListFeed.Error<Recipe>(
			        new HttpRequestException("503 Service Unavailable")));
			""");

	private void OnVmRefreshing(object sender, RoutedEventArgs e)
		=> ShowVm(
			MockRecipesViewModel.CreateMock(m =>
			{
				m.Recipes = MockListFeed.Refreshing(MockRecipesModel.SampleRecipes.ToArray());
				m.Save = MockCommand.Executing();
			}),
			"""
			VmHost.DataContext = MockRecipesViewModel.CreateMock(m =>
			{
			    m.Recipes = MockListFeed.Refreshing(sampleRecipes);
			    m.Save    = MockCommand.Executing();  // Save button stays disabled
			});
			""");

	private void ShowVm(MockRecipesViewModel vm, string code)
	{
		var previous = VmHost.Content as IAsyncDisposable;

		// Re-inflate the template so a fresh FeedView renders each state (see ShowDirect).
		VmHost.ContentTemplate = null;
		VmHost.Content = vm;
		VmHost.ContentTemplate = (DataTemplate)Resources["MockVmTemplate"];
		VmCode.Text = code;

		// A replaced view model owns feed subscriptions — release it (fire-and-forget, guarded).
		_ = DisposeAsync(previous);
	}

	private static async Task DisposeAsync(IAsyncDisposable? previous)
	{
		try
		{
			if (previous is not null)
			{
				await previous.DisposeAsync();
			}
		}
		catch (Exception)
		{
			// Disposing a replaced mock must never take the gallery down.
		}
	}
	#endregion
}
