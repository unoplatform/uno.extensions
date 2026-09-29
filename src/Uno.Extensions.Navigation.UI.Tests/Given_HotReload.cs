#if DEBUG // Hot-reload tests are only relevant in debug configuration
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

[TestClass]
[RunsInSecondaryApp(ignoreIfNotSupported: true)]
public class Given_HotReload
{
	[TestInitialize]
	public void Setup()
	{
		// Allow more time for the dev-server to load the Roslyn workspace (solution can be large)
		HotReloadHelper.DefaultWorkspaceTimeout = TimeSpan.FromSeconds(300);
		// Allow more time for the first metadata update (delta compilation can be slow on CI)
		HotReloadHelper.DefaultMetadataUpdateTimeout = TimeSpan.FromSeconds(60);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_NavigateAfterSourceUpdate_Then_NewPageReflectsUpdate(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			registerViewsAndRoutes: (views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadPageTwo>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("HotReloadPageTwo", View: views.FindByView<HotReloadPageTwo>()),
					}));
			},
			initialRoute: "HotReloadPageOne",
			ct);

		var page1 = ResolveCurrentPage<HotReloadPageOne>(app.NavigationRoot);
		page1.Should().NotBeNull("Frame should have navigated to HotReloadPageOne");
		page1!.DisplayedValue.Should().Be("original");

		// Apply the hot-reload source change. Disposal on scope-exit reverts the file.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Navigate to a fresh page — its constructor must observe the updated method body.
		await app.FrameNavigator.NavigateRouteAsync(this, "HotReloadPageTwo");

		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageTwo", TimeSpan.FromSeconds(30), ct);

		var page2 = ResolveCurrentPage<HotReloadPageTwo>(app.NavigationRoot);
		page2.Should().NotBeNull("Frame should have navigated to HotReloadPageTwo");
		page2!.DisplayedValue.Should().Be("updated");
	}

	/// <summary>
	/// Proves that a hot-reload change to a ViewModel method body — where the VM is wired to the
	/// page via <c>ViewMap&lt;TView, TViewModel&gt;()</c> and a <c>RouteMap</c> — is picked up on
	/// re-navigation. <c>HotReloadVm.DisplayedValue</c> is a property whose getter calls the HR'd
	/// <c>GetDisplayedValue()</c> method every read, so HR reflection is visible whether the Page
	/// is re-instantiated on back nav or retrieved from the Frame's cache.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_UpdateViewModel_Then_ReNavigationReflectsVmChange(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			registerViewsAndRoutes: (views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadVmPage, HotReloadVm>(),
					new ViewMap<HotReloadPageTwo>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadVmPage", View: views.FindByView<HotReloadVmPage>(), IsDefault: true),
						new RouteMap("HotReloadPageTwo", View: views.FindByView<HotReloadPageTwo>()),
					}));
			},
			initialRoute: "HotReloadVmPage",
			ct);

		var page = ResolveCurrentPage<HotReloadVmPage>(app.NavigationRoot);
		page.Should().NotBeNull("Frame should have navigated to HotReloadVmPage");
		page!.DataContext.Should().BeOfType<HotReloadVm>(
			"ViewMap<HotReloadVmPage, HotReloadVm> should have bound the VM as DataContext");
		page.DisplayedValue.Should().Be("original");

		// Apply the hot-reload source change to the VM's helper method. Disposal reverts the file.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/ViewModels/HotReloadVm.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Forward-nav to a sibling, then back. Back-nav is the canonical way to return to a
		// previously-visited page; forward-navigating to a route already on the back stack
		// behaves inconsistently in FrameNavigator.
		await app.FrameNavigator.NavigateRouteAsync(this, "HotReloadPageTwo");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageTwo", TimeSpan.FromSeconds(30), ct);

		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadVmPage", TimeSpan.FromSeconds(30), ct);

		var refreshedPage = ResolveCurrentPage<HotReloadVmPage>(app.NavigationRoot);
		refreshedPage.Should().NotBeNull("Frame should have navigated back to HotReloadVmPage");
		refreshedPage!.DataContext.Should().BeOfType<HotReloadVm>(
			"HotReloadVm should still be bound on the returned page");
		refreshedPage.DisplayedValue.Should().Be("updated");
	}

	/// <summary>
	/// Proves that a hot-reload change to the method a <c>RouteMap.Init</c> delegate targets can
	/// unlock a previously-gated route. Route registration itself is one-shot (the
	/// <c>RouteResolver</c> snapshots <c>IRouteRegistry.Items</c> at construction), so we cannot
	/// literally add a new <c>RouteMap</c> via HR. Instead, a pre-registered <c>"NewPage"</c>
	/// route has an <c>Init</c> delegate that calls <see cref="HotReloadRouteGate.IsAvailable"/>:
	/// when it returns <c>false</c> the delegate rewrites the request to redirect to
	/// <c>HotReloadPageOne</c>; once HR flips the method to <c>true</c>, the Init passes the
	/// request through and navigation resolves to <c>HotReloadPageTwo</c>.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_UpdateRouteInitGate_Then_GatedRouteBecomesNavigable(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			registerViewsAndRoutes: (views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadPageTwo>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap(
							"NewPage",
							View: views.FindByView<HotReloadPageTwo>(),
							Init: request =>
								HotReloadRouteGate.IsAvailable()
									? request
									: request with { Route = request.Route with { Base = "HotReloadPageOne" } }),
					}));
			},
			initialRoute: "HotReloadPageOne",
			ct);

		// Baseline: gate closed, navigating to "NewPage" should be redirected by the Init delegate
		// to "HotReloadPageOne", so HotReloadPageTwo must never appear.
		await app.FrameNavigator.NavigateRouteAsync(this, "NewPage");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);
		ResolveCurrentPage<HotReloadPageTwo>(app.NavigationRoot).Should().BeNull(
			"while the Init gate is closed, NewPage should redirect away and HotReloadPageTwo should not be shown");

		// HR: open the gate. Disposal reverts the file on scope exit.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadRouteGate.cs",
			"return false;",
			"return true;",
			ct);

		// Post-HR: gate is open, Init now passes the request through unchanged so NewPage resolves
		// to its registered view (HotReloadPageTwo).
		await app.FrameNavigator.NavigateRouteAsync(this, "NewPage");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "NewPage", TimeSpan.FromSeconds(30), ct);
		ResolveCurrentPage<HotReloadPageTwo>(app.NavigationRoot).Should().NotBeNull(
			"with the gate open post-HR, NewPage should resolve to HotReloadPageTwo");
	}

	/// <summary>
	/// Proves hot-reload works across Visibility-based region navigation. The host page
	/// <see cref="HotReloadRegionPage"/> wraps an empty Panel with <c>Region.Navigator="Visibility"</c>;
	/// <see cref="Uno.Extensions.Navigation.Navigators.PanelVisiblityNavigator"/> materializes a
	/// FrameView per navigated route into that panel. Each region route resolves to a fresh
	/// <see cref="HotReloadRegionContentPage"/> whose <see cref="HotReloadRegionVm"/> DataContext
	/// reads from <see cref="HotReloadRegionTarget.GetValue"/>. After the HR delta flips the target
	/// from "original" to "updated", switching to RegionTwo lands on a VM that sees the new value.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_SwitchRegionAfterUpdate_Then_NewlyShownRegionReflectsUpdate(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			registerViewsAndRoutes: (views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadRegionPage>(),
					new ViewMap<HotReloadRegionContentPage, HotReloadRegionVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadRegionPage",
							View: views.FindByView<HotReloadRegionPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("RegionOne", View: views.FindByView<HotReloadRegionContentPage>(), IsDefault: true),
								new RouteMap("RegionTwo", View: views.FindByView<HotReloadRegionContentPage>()),
							}),
					}));
			},
			initialRoute: "HotReloadRegionPage",
			ct);

		var hostPage = ResolveCurrentPage<HotReloadRegionPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadRegionPage");

		// RegionOne is the IsDefault nested route — the initial navigation's default-descent
		// (Navigator.DefaultRouteRequest) activates it automatically, so its FrameView is already
		// materialized inside ContentGrid by the time we land here.
		var regionOneVm = await WaitForRegionVmAsync(hostPage!.ContentGrid, "RegionOne", TimeSpan.FromSeconds(30), ct);
		regionOneVm.DisplayedValue.Should().Be("original",
			"RegionOne's VM should read the pre-HR method body");

		// Drive region switches through the ContentGrid's own navigator (PanelVisiblityNavigator),
		// not the outer FrameNavigator. The outer FrameNavigator rejects nested region routes at
		// RegionCanNavigate (route-map parent mismatch) and the request then falls into a bubble-up
		// path that doesn't cleanly resolve back into the panel, hanging the await. Calling the
		// panel navigator directly is the shape real app code would take for "I'm inside a
		// visibility-managed region and want to swap which named child is showing."
		var panelNavigator = await WaitForPanelNavigatorAsync(hostPage.ContentGrid, TimeSpan.FromSeconds(30), ct);

		// Sanity: region switching itself must work before we bring HR into the mix.
		await panelNavigator.NavigateRouteAsync(hostPage, "RegionTwo");
		var regionTwoBefore = await WaitForRegionVmAsync(hostPage.ContentGrid, "RegionTwo", TimeSpan.FromSeconds(30), ct);
		regionTwoBefore.DisplayedValue.Should().Be("original", "RegionTwo (pre-HR) should also read 'original'");

		// HR: flip the region target's helper method. Disposal reverts the file on scope exit.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadRegionTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Switch back to RegionOne, then forward to RegionTwo — re-showing RegionTwo exercises the
		// "newly shown region" angle. PanelVisiblityNavigator reuses the existing FrameView/VM, and
		// because HotReloadRegionVm.DisplayedValue calls GetValue() on every access, the reused VM
		// now returns "updated".
		await panelNavigator.NavigateRouteAsync(hostPage, "RegionOne");
		await panelNavigator.NavigateRouteAsync(hostPage, "RegionTwo");

		var regionTwoVm = await WaitForRegionVmAsync(hostPage.ContentGrid, "RegionTwo", TimeSpan.FromSeconds(30), ct);
		regionTwoVm.DisplayedValue.Should().Be("updated",
			"RegionTwo's VM should read the post-HR method body");
	}

	/// <summary>
	/// Regression test for the cascade-after-HR fix in <see cref="NavigationRouteUpdateHandler"/>:
	/// after a C# hot-reload triggers <c>UpdateApplication</c>, the resolver-rebuild path
	/// dispatches a cascade walk that re-evaluates nested <c>IsDefault</c> routes. The walk
	/// must NOT clobber a non-default region that the user has already navigated to.
	/// <para>
	/// Without <c>FindActiveDescendantNestedRoute</c>, the cascade would re-dispatch
	/// <c>RegionOne</c> (the <c>IsDefault</c> nested route) over the user's active
	/// <c>RegionTwo</c> selection, producing a "blank content area / unexpected jump"
	/// symptom on every HR delta.
	/// </para>
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_HRCascadeAfterUserSelection_Then_ActiveRegionPreserved(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			registerViewsAndRoutes: (views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadRegionPage>(),
					new ViewMap<HotReloadRegionContentPage, HotReloadRegionVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadRegionPage",
							View: views.FindByView<HotReloadRegionPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("RegionOne", View: views.FindByView<HotReloadRegionContentPage>(), IsDefault: true),
								new RouteMap("RegionTwo", View: views.FindByView<HotReloadRegionContentPage>()),
							}),
					}));
			},
			initialRoute: "HotReloadRegionPage",
			ct);

		var hostPage = ResolveCurrentPage<HotReloadRegionPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadRegionPage");

		// Drive the panel navigator off RegionOne (IsDefault) onto RegionTwo so the active
		// nested route diverges from the IsDefault. The cascade-after-HR fix must respect
		// this divergence.
		//
		// Note on verification: PanelVisiblityNavigator's own Route.Base is empty after
		// navigating to a Page-subclass route — Show() wraps the page in a FrameView,
		// returns null, and ExecuteRequestAsync falls into the Route.Empty branch
		// (see ControlNavigator.cs). The "active region" is what the panel makes
		// visible (PostNavigateAsync sets Visibility.Visible on the active child and
		// Collapsed on the rest), so we assert on the visible child's Region.Name.
		var panelNavigator = await WaitForPanelNavigatorAsync(hostPage!.ContentGrid, TimeSpan.FromSeconds(30), ct);
		await panelNavigator.NavigateRouteAsync(hostPage, "RegionTwo");
		await WaitForRegionVmAsync(hostPage.ContentGrid, "RegionTwo", TimeSpan.FromSeconds(30), ct);
		GetActiveRegionName(hostPage.ContentGrid).Should().Be("RegionTwo",
			"Pre-HR: PanelVisiblityNavigator should be showing RegionTwo after explicit navigation");

		// Apply any HR source change to trigger UpdateApplication. The cascade dispatched
		// by RebuildRoutes -> ScheduleCascade -> CascadeNewDefaultsFromRoot walks the live
		// region tree. The fix ensures that walk respects RegionTwo's active selection
		// and does NOT re-issue an IsDefault navigation back to RegionOne.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadRegionTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Allow time for UpdateApplication + dispatched cascade walk to complete. The
		// fix's TryEnqueue defers the walk onto the dispatcher; we need to wait long
		// enough for the dispatched lambda to run.
		await Task.Delay(1000, ct);

		GetActiveRegionName(hostPage.ContentGrid).Should().Be("RegionTwo",
			"Post-HR: the cascade walk must preserve RegionTwo (active selection) and NOT " +
			"re-dispatch the IsDefault RegionOne. Without the FindActiveDescendantNestedRoute " +
			"check, the cascade would clobber the user's selection on every HR delta.");
	}

	/// <summary>
	/// Regression test for uno.extensions#3142: a hot-reload delta that updates the view model
	/// of a region's ACTIVE route must re-instantiate that view model in place. Constructor /
	/// property-initializer edits are only visible on a fresh instance, and before the fix no HR
	/// path created one: the IsDefault cascade deliberately suppresses regions already on their
	/// route (<c>FindActiveDescendantNestedRoute</c>), and the frame-content re-hook only reacts
	/// to element replacement — so the one page guaranteed not to refresh was the page the user
	/// was looking at. The refresh must also NOT disturb the user's selection: RegionTwo
	/// (non-default) stays active, guarding the same no-yank behavior as
	/// <see cref="When_HRCascadeAfterUserSelection_Then_ActiveRegionPreserved"/>.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ActiveRouteViewModelUpdated_Then_ActiveRegionVmReinstantiated(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			registerViewsAndRoutes: (views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadRegionPage>(),
					new ViewMap<HotReloadRegionContentPage, HotReloadRegionVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadRegionPage",
							View: views.FindByView<HotReloadRegionPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("RegionOne", View: views.FindByView<HotReloadRegionContentPage>(), IsDefault: true),
								new RouteMap("RegionTwo", View: views.FindByView<HotReloadRegionContentPage>()),
							}),
					}));
			},
			initialRoute: "HotReloadRegionPage",
			ct);

		var hostPage = ResolveCurrentPage<HotReloadRegionPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadRegionPage");

		// Move off the IsDefault route so the test also proves the refresh does not re-issue
		// the IsDefault cascade (RegionTwo must stay active throughout).
		var panelNavigator = await WaitForPanelNavigatorAsync(hostPage!.ContentGrid, TimeSpan.FromSeconds(30), ct);
		await panelNavigator.NavigateRouteAsync(hostPage, "RegionTwo");
		var vmBefore = await WaitForRegionVmAsync(hostPage.ContentGrid, "RegionTwo", TimeSpan.FromSeconds(30), ct);
		vmBefore.CtorSeededValue.Should().Be("ctor-original",
			"precondition: the pre-HR view model must carry the pre-HR constructor seed");

		// HR: edit the ACTIVE route's view-model type itself (not a helper class), so the delta
		// contains a navigation-registered view-model type. The edited method only runs from the
		// property initializer, so the change is invisible unless a new instance is created.
		// Disposal reverts the file on scope exit.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/ViewModels/HotReloadRegionVm.cs",
			"""return "ctor-original";""",
			"""return "ctor-updated";""",
			ct);

		// The refresh is dispatched onto the dispatcher and applied fire-and-forget; poll for
		// the re-created view model on the active region.
		var refreshedVm = await WaitForReinstantiatedRegionVmAsync(
			hostPage.ContentGrid, "RegionTwo", vmBefore, TimeSpan.FromSeconds(30), ct);
		refreshedVm.CtorSeededValue.Should().Be("ctor-updated",
			"the active route's view model must be re-instantiated so constructor and " +
			"property-initializer edits become visible (#3142)");

		GetActiveRegionName(hostPage.ContentGrid).Should().Be("RegionTwo",
			"the refresh must re-create the view model in place without yanking the selection " +
			"back to the IsDefault RegionOne");
	}

	/// <summary>
	/// Deterministic repro for unoplatform/uno.extensions#3130 (RED until the stranded-page
	/// fix lands). A page that is live but NOT materialized — its host panel is Collapsed,
	/// the deterministic stand-in for "the hosted app's view gets no layout pass while an
	/// external tool fills its pages" proven in the WASM investigation — exists only as
	/// <c>Frame.Content</c>, never as a visual child. Uno's HR visual-tree walk enumerates
	/// <c>VisualTreeHelper</c> children only, so a XAML hot reload of that page's type
	/// replaces nothing, and navigation keeps the stale instance (the keep-active-instance
	/// cascade skip + "no segments to navigate" both decline to refresh). Revealing the
	/// panel afterwards shows the pre-HR placeholder — the "default tab renders its
	/// scaffolded placeholder" symptom from the original report.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PageXamlUpdatedWhileUnmaterialized_Then_RevealShowsUpdatedContent(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			registerViewsAndRoutes: (views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadRegionPage>(),
					new ViewMap<HotReloadStrandedContentPage>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadRegionPage",
							View: views.FindByView<HotReloadRegionPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								// Deliberately NOT IsDefault: the test collapses the panel
								// first and then populates it, so the page is created while
								// the region cannot be laid out.
								new RouteMap("Stranded", View: views.FindByView<HotReloadStrandedContentPage>()),
							}),
					}));
			},
			initialRoute: "HotReloadRegionPage",
			ct);

		var hostPage = ResolveCurrentPage<HotReloadRegionPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadRegionPage");

		var panelNavigator = await WaitForPanelNavigatorAsync(hostPage!.ContentGrid, TimeSpan.FromSeconds(30), ct);

		// Hide the content area BEFORE populating it. Collapsed skips measure, so anything
		// created inside never applies its template and never fires Loaded.
		hostPage.ContentGrid.Visibility = Visibility.Collapsed;

		// Fire the navigation without awaiting: with a collapsed panel the pipeline stalls
		// at CheckLoadedAsync/EnsureLoaded (the FrameView never loads) — the same stall the
		// WASM repro showed for 9.5 minutes. The page instance is still created synchronously
		// enough for the poll below. Observe the task so a later fault is not unobserved.
		var navTask = panelNavigator.NavigateRouteAsync(hostPage, "Stranded");
		_ = navTask.ContinueWith(static t => t.Exception?.GetBaseException(), TaskScheduler.Default);

		var strandedFrame = await WaitForStrandedFrameAsync(hostPage.ContentGrid, TimeSpan.FromSeconds(30), ct);
		var stalePage = (HotReloadStrandedContentPage)strandedFrame.Content;

		// Preconditions — this is the #3130 state; if these fail the harness is not
		// producing the live-but-unmaterialized condition and the test proves nothing.
		VisualDescendants(strandedFrame).Should().NotContain(stalePage,
			"precondition: the page must exist only as Frame.Content, not as a visual child");
		stalePage.Status?.Text.Should().Be("placeholder", "precondition: pre-HR XAML content");
		navTask.IsFaulted.Should().BeFalse("the stalled navigation must not have faulted");

		// XAML HR: fill the placeholder while the page is live but unmaterialized. The helper
		// awaits delta delivery; disposal reverts the file on scope exit.
		await using var fileRevert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadStrandedContentPage.xaml",
			"Text=\"placeholder\"",
			"Text=\"filled\"",
			ct);

		// Give the HR visual-tree update phase (dispatched onto the UI thread) time to run.
		await Task.Delay(2000, ct);

		// Reveal — the "user opens the App tab" moment.
		hostPage.ContentGrid.Visibility = Visibility.Visible;

		// Wait for ANY materialized HotReloadStrandedContentPage: the fix is allowed to swap
		// the instance, so the test must not pin the stale reference here.
		var visiblePage = await WaitForMaterializedPageAsync<HotReloadStrandedContentPage>(
			hostPage.ContentGrid, TimeSpan.FromSeconds(30), ct);

		// THE assertion this test exists for (red on main): content hot-reloaded while the
		// page was unmaterialized must be visible once the page is revealed.
		visiblePage.Status?.Text.Should().Be("filled",
			"a page whose XAML was hot-reloaded while it was live-but-unmaterialized must show " +
			"the updated content once revealed (#3130: the HR walk misses Frame.Content of a " +
			"never-laid-out frame and navigation never refreshes the stale instance)");
	}

	private static async Task<Frame> WaitForStrandedFrameAsync(
		Grid contentGrid,
		TimeSpan timeout,
		CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			var frameView = contentGrid.Children
				.OfType<FrameView>()
				.FirstOrDefault(fv => Uno.Extensions.Navigation.UI.Region.GetName(fv) == "Stranded");
			if (frameView?.FindName("NavigationFrame") is Frame frame &&
				frame.Content is HotReloadStrandedContentPage)
			{
				return frame;
			}
			await Task.Delay(50, ct);
		}

		var children = string.Join(", ", contentGrid.Children
			.OfType<FrameworkElement>()
			.Select(c => $"{c.GetType().Name}[Region.Name='{Uno.Extensions.Navigation.UI.Region.GetName(c)}']"));
		throw new TimeoutException(
			$"The stranded page did not get created as Frame.Content within {timeout.TotalSeconds:F0}s. " +
			$"ContentGrid children: [{children}].");
	}

	private static async Task<TPage> WaitForMaterializedPageAsync<TPage>(
		Grid contentGrid,
		TimeSpan timeout,
		CancellationToken ct)
		where TPage : FrameworkElement
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (VisualDescendants(contentGrid).OfType<TPage>().FirstOrDefault(p => p.IsLoaded) is { } page)
			{
				return page;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"No materialized {typeof(TPage).Name} appeared within {timeout.TotalSeconds:F0}s of revealing the panel.");
	}

	private static System.Collections.Generic.IEnumerable<DependencyObject> VisualDescendants(DependencyObject root)
	{
		var count = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(root, i);
			yield return child;
			foreach (var grandChild in VisualDescendants(child))
			{
				yield return grandChild;
			}
		}
	}

	private static string? GetActiveRegionName(Grid contentGrid)
		=> contentGrid.Children
			.OfType<FrameworkElement>()
			.Where(c => c.Visibility == Visibility.Visible)
			.Select(c => Uno.Extensions.Navigation.UI.Region.GetName(c))
			.FirstOrDefault(name => !string.IsNullOrEmpty(name));

	private static async Task<global::Uno.Extensions.Navigation.INavigator> WaitForPanelNavigatorAsync(
		Grid contentGrid,
		TimeSpan timeout,
		CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (contentGrid.Navigator() is { } nav)
			{
				return nav;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"ContentGrid's navigator (PanelVisiblityNavigator) did not become available within {timeout.TotalSeconds:F0}s.");
	}

	private static async Task<HotReloadRegionVm> WaitForRegionVmAsync(
		Grid contentGrid,
		string regionName,
		TimeSpan timeout,
		CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			var regionView = contentGrid.Children
				.OfType<FrameworkElement>()
				.FirstOrDefault(c => Uno.Extensions.Navigation.UI.Region.GetName(c) == regionName);
			if (regionView is FrameView fv &&
				fv.FindName("NavigationFrame") is Frame frame &&
				frame.Content is HotReloadRegionContentPage page &&
				page.DataContext is HotReloadRegionVm vm)
			{
				return vm;
			}
			await Task.Delay(50, ct);
		}

		var children = string.Join(", ", contentGrid.Children
			.OfType<FrameworkElement>()
			.Select(c => $"{c.GetType().Name}[Region.Name='{Uno.Extensions.Navigation.UI.Region.GetName(c)}']"));
		throw new TimeoutException(
			$"Region '{regionName}' did not populate a HotReloadRegionContentPage within {timeout.TotalSeconds:F0}s. " +
			$"ContentGrid children: [{children}].");
	}

	/// <summary>
	/// Like <see cref="WaitForRegionVmAsync"/>, but only returns once the region's view model is
	/// a DIFFERENT instance than <paramref name="previousVm"/> — the hot-reload refresh replaces
	/// the DataContext asynchronously, so polling for type alone would return the stale instance.
	/// </summary>
	private static async Task<HotReloadRegionVm> WaitForReinstantiatedRegionVmAsync(
		Grid contentGrid,
		string regionName,
		HotReloadRegionVm previousVm,
		TimeSpan timeout,
		CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			var regionView = contentGrid.Children
				.OfType<FrameworkElement>()
				.FirstOrDefault(c => Uno.Extensions.Navigation.UI.Region.GetName(c) == regionName);
			if (regionView is FrameView fv &&
				fv.FindName("NavigationFrame") is Frame frame &&
				frame.Content is HotReloadRegionContentPage page &&
				page.DataContext is HotReloadRegionVm vm &&
				!ReferenceEquals(vm, previousVm))
			{
				return vm;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"Region '{regionName}' still exposes the pre-HR view model instance after {timeout.TotalSeconds:F0}s — " +
			"the active route's view model was not re-instantiated (#3142).");
	}

	/// <summary>
	/// Boots an Uno host with navigation, hosts it in the runtime-tests engine's already-displayed
	/// test window, and navigates to <paramref name="initialRoute"/>. Disposal stops the host and
	/// restores the window's original content.
	/// </summary>
	/// <remarks>
	/// Creating a fresh <c>new Window()</c> in <c>RunsInSecondaryApp</c> mode produces an
	/// un-composited window whose Loaded/Activate events never fire, which prevents initial
	/// navigation from running — the symptom is a black secondary app. We reuse
	/// <see cref="UnitTestsUIContentHelper.CurrentTestWindow"/> to avoid that.
	///
	/// We navigate directly to <paramref name="initialRoute"/> rather than relying on root
	/// "" → IsDefault descent. Other tests in this project (Given_ChainedGetDataAsync,
	/// Given_RouteNotifier) follow this pattern; descending from the empty root requires a nested
	/// Region.Attached ContentControl, which we don't have here.
	/// </remarks>
	private static async Task<HotReloadTestApp> SetupAppAsync(
		Action<global::Uno.Extensions.Navigation.IViewRegistry, global::Uno.Extensions.Navigation.IRouteRegistry> registerViewsAndRoutes,
		string initialRoute,
		CancellationToken ct)
	{
		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		var navigationRoot = new ContentControl
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch,
		};

		UnitTestsUIContentHelper.SaveOriginalContent();
		window.Content = navigationRoot;

		IHost? host = null;
		try
		{
			host = await window.InitializeNavigationAsync(
				buildHost: async () => UnoHost
					.CreateDefaultBuilder(typeof(Given_HotReload).Assembly)
					.UseNavigation(viewRouteBuilder: registerViewsAndRoutes)
					.Build(),
				navigationRoot: navigationRoot,
				initialRoute: initialRoute);

			// When navigating a Page into a ContentControl root, ContentControlNavigator wraps the
			// Page in a FrameView (see ContentControlNavigator.Show). The Page ends up in the
			// FrameView's inner Frame, and the Frame's navigator is what tracks the route — so we
			// look at the FrameView's Navigator, not the root ContentControl's.
			var frameNav = await WaitForFrameNavigatorAsync(navigationRoot, TimeSpan.FromSeconds(30), ct);
			await WaitForRouteAsync(navigationRoot, frameNav, initialRoute, TimeSpan.FromSeconds(30), ct);

			return new HotReloadTestApp(navigationRoot, frameNav, host);
		}
		catch
		{
			if (host is not null)
			{
				await host.StopAsync();
			}
			UnitTestsUIContentHelper.RestoreOriginalContent();
			throw;
		}
	}

	private sealed class HotReloadTestApp : IAsyncDisposable
	{
		private readonly IHost _host;

		public HotReloadTestApp(ContentControl navigationRoot, global::Uno.Extensions.Navigation.INavigator frameNavigator, IHost host)
		{
			NavigationRoot = navigationRoot;
			FrameNavigator = frameNavigator;
			_host = host;
		}

		public ContentControl NavigationRoot { get; }

		public global::Uno.Extensions.Navigation.INavigator FrameNavigator { get; }

		public async ValueTask DisposeAsync()
		{
			try
			{
				await _host.StopAsync();
			}
			finally
			{
				UnitTestsUIContentHelper.RestoreOriginalContent();
			}
		}
	}

	private static async Task<global::Uno.Extensions.Navigation.INavigator> WaitForFrameNavigatorAsync(
		ContentControl root,
		TimeSpan timeout,
		CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (root.Content is FrameView fv && fv.Navigator is { } nav)
			{
				return nav;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"FrameView navigator did not become available within {timeout.TotalSeconds:F0}s. " +
			$"root.Content={root.Content?.GetType().FullName ?? "<null>"}.");
	}

	private static TPage? ResolveCurrentPage<TPage>(ContentControl root) where TPage : class
	{
		if (root.Content is FrameView fv && fv.FindName("NavigationFrame") is Frame frame)
		{
			return frame.Content as TPage;
		}
		return root.Content as TPage;
	}

	private static async Task WaitForRouteAsync(
		ContentControl root,
		global::Uno.Extensions.Navigation.INavigator nav,
		string expectedBase,
		TimeSpan timeout,
		CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (nav.Route?.Base == expectedBase)
			{
				return;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"Navigation did not reach Base='{expectedBase}' within {timeout.TotalSeconds:F0}s. " +
			$"Last state: Route='{nav.Route?.Base ?? "<null>"}', " +
			$"root.Content={root.Content?.GetType().FullName ?? "<null>"}.");
	}
}
#endif
