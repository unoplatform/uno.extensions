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
	/// Proves hot-reload works across the canonical "Use a Panel to switch views" pattern from the
	/// <c>HowTo-UsePanel</c> docs: a <see cref="Grid"/> region with <c>Region.Navigator="Visibility"</c>
	/// containing pre-existing inline children identified by <c>Region.Name</c>. Unlike
	/// <see cref="When_SwitchRegionAfterUpdate_Then_NewlyShownRegionReflectsUpdate"/> — which routes
	/// pages into a panel via <c>RouteMap</c> + <c>FrameView</c> materialization —
	/// <see cref="HotReloadPanelHostPage"/> hands the navigator children that already exist, so
	/// <see cref="Uno.Extensions.Navigation.Navigators.PanelVisiblityNavigator.Show"/> takes the
	/// <c>FindByPath</c> branch and just toggles <see cref="UIElement.Visibility"/>. Each inline
	/// child re-reads <see cref="HotReloadPanelTarget.GetValue"/> on every Collapsed→Visible
	/// transition; that re-read is what surfaces the HR delta when we switch to a fresh region.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_SwitchInlinePanelRegionAfterUpdate_Then_NewlyShownRegionReflectsUpdate(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			registerViewsAndRoutes: (views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPanelHostPage>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadPanelHostPage",
							View: views.FindByView<HotReloadPanelHostPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								// Inline panel children are matched by Region.Name in
								// PanelVisiblityNavigator.FindByPath, so the View on these nested
								// route maps is intentionally null — they exist only to give the
								// resolver a known route name and to mark the default region.
								new RouteMap("One", IsDefault: true),
								new RouteMap("Two"),
								new RouteMap("Three"),
							}),
					}));
			},
			initialRoute: "HotReloadPanelHostPage",
			ct);

		var hostPage = ResolveCurrentPage<HotReloadPanelHostPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadPanelHostPage");

		// Default-descent should activate the IsDefault nested region "One". The visibility callback
		// on RegionOne fires on Collapsed→Visible and writes the pre-HR target value.
		await WaitForInlineRegionTextAsync(hostPage!.RegionOne, "original", TimeSpan.FromSeconds(30), ct);

		// Drive the region switch through the panel's own navigator (PanelVisiblityNavigator), not
		// the outer FrameNavigator — the outer one rejects nested region routes at
		// RegionCanNavigate (route-map parent mismatch) and the bubble-up hangs the await. Same
		// reasoning as the route-materialized region test above.
		var panelNavigator = await WaitForPanelNavigatorAsync(hostPage.PanelRoot, TimeSpan.FromSeconds(30), ct);

		// HR while RegionTwo is still Collapsed (it has never been shown, so its TextBlock is empty
		// and the visibility callback has never fired). After the delta lands, switching to "Two"
		// makes its first Collapsed→Visible transition read the updated method body.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadPanelTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		await panelNavigator.NavigateRouteAsync(hostPage, "Two");

		await WaitForInlineRegionTextAsync(hostPage.RegionTwo, "updated", TimeSpan.FromSeconds(30), ct);

		// Sanity check: switching to Two must actually have flipped One back to Collapsed —
		// otherwise PostNavigateAsync didn't run and the test isn't really exercising the navigator.
		hostPage.RegionOne.Visibility.Should().Be(Visibility.Collapsed,
			"PanelVisiblityNavigator.PostNavigateAsync should have collapsed the previously-visible region");
	}

	/// <summary>
	/// Complement to <see cref="When_SwitchInlinePanelRegionAfterUpdate_Then_NewlyShownRegionReflectsUpdate"/>.
	/// Switches to a sibling region pre-HR (so it's already been materialized once with the original
	/// value), applies the HR delta, then switches back to the original region. The inline child
	/// instances are reused — there is no re-instantiation — so the assertion proves the
	/// "refresh on visibility transition" callback re-reads the HR target on every Collapsed→Visible
	/// flip, not just on first show. This is the load-bearing scenario for HR + inline panel
	/// content because most apps switch back and forth between sections rather than only navigating
	/// forward into never-seen regions.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ReturnToInlinePanelRegionAfterUpdate_Then_RevisitedRegionReflectsUpdate(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			registerViewsAndRoutes: (views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPanelHostPage>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadPanelHostPage",
							View: views.FindByView<HotReloadPanelHostPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("One", IsDefault: true),
								new RouteMap("Two"),
								new RouteMap("Three"),
							}),
					}));
			},
			initialRoute: "HotReloadPanelHostPage",
			ct);

		var hostPage = ResolveCurrentPage<HotReloadPanelHostPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadPanelHostPage");

		await WaitForInlineRegionTextAsync(hostPage!.RegionOne, "original", TimeSpan.FromSeconds(30), ct);

		var panelNavigator = await WaitForPanelNavigatorAsync(hostPage.PanelRoot, TimeSpan.FromSeconds(30), ct);

		// Pre-HR: switch to "Two" so we have an established baseline that the panel switch works
		// and the inline callback reads the unmodified target.
		await panelNavigator.NavigateRouteAsync(hostPage, "Two");
		await WaitForInlineRegionTextAsync(hostPage.RegionTwo, "original", TimeSpan.FromSeconds(30), ct);

		// HR after both regions have been shown at least once — we're proving that re-entering an
		// already-loaded inline child still picks up the new method body, since the visibility
		// callback re-reads the target on every Collapsed→Visible transition.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadPanelTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		await panelNavigator.NavigateRouteAsync(hostPage, "One");

		await WaitForInlineRegionTextAsync(hostPage.RegionOne, "updated", TimeSpan.FromSeconds(30), ct);

		hostPage.RegionTwo.Visibility.Should().Be(Visibility.Collapsed,
			"PanelVisiblityNavigator.PostNavigateAsync should have collapsed Two when switching back to One");
	}

	private static async Task WaitForInlineRegionTextAsync(
		HotReloadPanelHostPage.InlineRegion region,
		string expectedText,
		TimeSpan timeout,
		CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (region.Text.Text == expectedText)
			{
				return;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"Inline region '{Uno.Extensions.Navigation.UI.Region.GetName(region)}' did not display " +
			$"'{expectedText}' within {timeout.TotalSeconds:F0}s. " +
			$"Last state: Visibility={region.Visibility}, Text='{region.Text.Text ?? "<null>"}'.");
	}

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
