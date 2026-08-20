using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.UI.Controls;
using Uno.Extensions.Navigation.UI.Tests.Pages;
using Uno.Extensions.Navigation.UI.Tests.ViewModels;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Tests for the hot-reload refresh of the ACTIVE route's view model
/// (uno.extensions#3142). A C# delta that updates the view model of the route a region is
/// currently on must re-create that view model in place: a metadata update never re-runs
/// constructors or property initializers on live instances, the IsDefault cascade deliberately
/// suppresses regions that are already on their route, and the frame-content re-hook only reacts
/// to element replacement — so before the fix, nothing refreshed the one page the user is
/// looking at.
/// <para>
/// Same technique as <see cref="Given_FrameContentRehook"/>: the delta is synthesized by invoking
/// the internal hot-reload entry point directly, so these tests run without the HR harness and
/// pin the refresh decision deterministically. The full-fidelity delta is covered by
/// <c>Given_HotReload.When_ActiveRouteViewModelUpdated_Then_ActiveRegionVmReinstantiated</c>.
/// </para>
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_ActiveRouteVmRefresh
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	private async Task<(IHost Host, Frame Frame)> SetupNavigationAsync()
	{
		var window = new Window();

		var host = await window.InitializeNavigationAsync(
			buildHost: async () => UnoHost
				.CreateDefaultBuilder(typeof(Given_ActiveRouteVmRefresh).Assembly)
				.UseNavigation(viewRouteBuilder: (views, routes) =>
				{
					views.Register(
						new ViewMap<HotReloadVmPage, HotReloadVm>());

					routes.Register(
						new RouteMap("", Nested: new RouteMap[]
						{
							new RouteMap("HotReloadVmPage", View: views.FindByView<HotReloadVmPage>(), IsDefault: true),
						}));
				})
				.Build(),
			initialRoute: "HotReloadVmPage");

		var root = (ContentControl)window.Content!;

		using var cts = new CancellationTokenSource(Timeout);
		await UIHelper.WaitFor(
			() => root.Content is FrameView fv &&
				fv.FindName("NavigationFrame") is Frame frame &&
				frame.Content is HotReloadVmPage page &&
				page.DataContext is HotReloadVm,
			cts.Token);

		var frameView = (FrameView)root.Content!;
		var frame = (Frame)frameView.FindName("NavigationFrame");

		// Precondition for every test in this class: the navigation context must be registered
		// with a live root region and resolver, otherwise the refresh walk has nothing to visit
		// and a "view model preserved" assertion would pass vacuously.
		NavigationRouteUpdateHandler.ActiveContexts
			.Should().Contain(
				ctx => ctx.Resolver != null && ctx.RootRegion != null,
				"the hot-reload walk operates on registered contexts with a live region tree");

		return (host, frame);
	}

	[TestMethod]
	public async Task When_ActiveRouteViewModelInDelta_Then_ViewModelRebuiltInPlace()
	{
		var (host, frame) = await SetupNavigationAsync();

		try
		{
			var page = (HotReloadVmPage)frame.Content;
			var originalVm = (HotReloadVm)page.DataContext;

			// The delta contains the view model mapped to the route the frame is currently on
			// (an in-place EnC update keeps the type identity). #3142: the region is already on
			// 'HotReloadVmPage', so no cascade or navigation will ever touch it — the refresh
			// walk is the only path that can re-run the view model's constructor.
			NavigationRouteUpdateHandler.ScheduleCascadeForAllContextsIfRouteRelevant(new[] { typeof(HotReloadVm) });

			using var cts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => page.DataContext is HotReloadVm vm && !ReferenceEquals(vm, originalVm),
				cts.Token);

			page.DataContext.Should().BeOfType<HotReloadVm>(
				"the active route's view model must be re-instantiated so constructor and " +
				"property-initializer edits become visible (#3142)");
			frame.Content.Should().BeSameAs(page,
				"the refresh re-creates the view model only — replacing the view is owned by " +
				"Uno's element-update walk");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	[TestMethod]
	public async Task When_DeltaContainsOnlyViewTypes_Then_ViewModelPreserved()
	{
		var (host, frame) = await SetupNavigationAsync();

		try
		{
			var page = (HotReloadVmPage)frame.Content;
			var originalVm = (HotReloadVm)page.DataContext;

			// A view-only delta (XAML or code-behind edit on the page type) must NOT rebuild the
			// view model: updated views are owned by Uno's element-update walk, and re-creating
			// the view model here would discard its un-persisted state on every page edit — the
			// same behavior Given_FrameContentRehook pins for the re-hook path.
			NavigationRouteUpdateHandler.ScheduleCascadeForAllContextsIfRouteRelevant(new[] { typeof(HotReloadVmPage) });

			// Nothing observable should change; give the dispatched walk time to run.
			await Task.Delay(500);

			page.DataContext.Should().BeSameAs(originalVm,
				"a delta without the mapped view-model type must preserve the live view model " +
				"and any un-persisted state it holds");

			// The walk must not probe FindByViewModel with view types: on a miss,
			// RouteResolverDefault's convention fallback derives the view's own route path
			// from the type name and REPLACES the registered mapping with one whose
			// ViewModel is the view type itself — corrupting every later navigation on
			// that route (the CI regression across the XAML TabBar HR tests).
			var context = NavigationRouteUpdateHandler.ActiveContexts.First(ctx => ctx.Resolver is not null);
			var mapping = context.Resolver!.FindByPath("HotReloadVmPage");
			mapping.Should().NotBeNull();
			mapping!.RenderView.Should().Be(typeof(HotReloadVmPage),
				"the registered route's view must survive a view-only delta");
			mapping.ViewModel.Should().Be(typeof(HotReloadVm),
				"the registered route's view model must survive a view-only delta — a " +
				"view-typed ViewModel here means the convention fallback clobbered the mapping");
		}
		finally
		{
			await host.StopAsync();
		}
	}
}
