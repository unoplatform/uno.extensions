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
	/// Proves that hot-reload applied to a modal (<see cref="Microsoft.UI.Xaml.Controls.ContentDialog"/>) view
	/// does not disturb the underlying Frame's navigation. <see cref="Uno.Extensions.Navigation.Navigators.ContentDialogNavigator"/>
	/// presents the dialog as a top-level overlay via <c>ShowAsync</c> — it does not push onto the
	/// Frame's back stack and does not replace the Frame's current page (<c>FrameNavigator.ExecuteRequestAsync</c>
	/// calls <c>CloseActiveClosableNavigators</c> only on frame nav, not when the modal opens). The test:
	/// navigates to <see cref="HotReloadPageOne"/>, shows the modal, applies an HR edit to the modal's
	/// target, re-shows the modal, and asserts throughout that the Frame's route stays pinned to the
	/// underlying page and that the underlying page instance/value is never mutated.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ShowModalAfterUpdate_Then_ModalReflectsUpdateAndFrameStaysStable(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			registerViewsAndRoutes: (views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadModalDialog>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("HotReloadModalDialog", View: views.FindByView<HotReloadModalDialog>()),
					}));
			},
			initialRoute: "HotReloadPageOne",
			ct);

		var page1 = ResolveCurrentPage<HotReloadPageOne>(app.NavigationRoot);
		page1.Should().NotBeNull("Frame should have navigated to HotReloadPageOne");
		page1!.DisplayedValue.Should().Be("original");

		// Pre-HR: show the modal. The "!" qualifier (or auto-detection via IsDialogViewType)
		// routes the request through ContentDialogNavigator.DisplayDialog, which builds a fresh
		// HotReloadModalDialog via Activator and calls ShowAsync on it. The Frame is untouched.
		await app.FrameNavigator.NavigateRouteAsync(this, "!HotReloadModalDialog");

		var modalBefore = await WaitForModalAsync(TimeSpan.FromSeconds(30), ct);
		modalBefore.DisplayedValue.Should().Be("original",
			"pre-HR modal ctor should capture the unchanged method body");
		app.FrameNavigator.Route?.Base.Should().Be("HotReloadPageOne",
			"showing a modal must not replace the Frame's underlying page");

		modalBefore.Hide();
		await WaitForModalClosedAsync(TimeSpan.FromSeconds(10), ct);

		ResolveCurrentPage<HotReloadPageOne>(app.NavigationRoot).Should().BeSameAs(page1,
			"closing the modal must not remount or replace the underlying page instance");
		app.FrameNavigator.Route?.Base.Should().Be("HotReloadPageOne",
			"closing the modal must leave the Frame's route untouched");

		// HR: flip the modal's target. Disposal reverts the file on scope exit.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadModalTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Post-HR: re-show the modal. ContentDialogNavigator constructs a fresh ContentDialog
		// per navigation (DisplayDialog → CreateInstance), so the new ctor runs against the
		// HR'd method body.
		await app.FrameNavigator.NavigateRouteAsync(this, "!HotReloadModalDialog");

		var modalAfter = await WaitForModalAsync(TimeSpan.FromSeconds(30), ct);
		modalAfter.Should().NotBeSameAs(modalBefore,
			"ContentDialogNavigator constructs a fresh dialog per navigation — the HR delta is observed via the new instance");
		modalAfter.DisplayedValue.Should().Be("updated",
			"post-HR modal ctor should read the updated method body");

		// Core claim: HR on the modal's target does not bleed into the underlying page or
		// the Frame's navigation state.
		page1.DisplayedValue.Should().Be("original",
			"underlying page's DisplayedValue was captured pre-HR and must remain unchanged");
		app.FrameNavigator.Route?.Base.Should().Be("HotReloadPageOne",
			"post-HR modal show must not replace the Frame's underlying page");

		modalAfter.Hide();
		await WaitForModalClosedAsync(TimeSpan.FromSeconds(10), ct);

		ResolveCurrentPage<HotReloadPageOne>(app.NavigationRoot).Should().BeSameAs(page1,
			"closing the post-HR modal must not remount the underlying page");
		app.FrameNavigator.Route?.Base.Should().Be("HotReloadPageOne",
			"Frame's Route must remain on HotReloadPageOne after the full modal+HR cycle");
	}

	/// <summary>
	/// Proves that a hot-reload edit to a <see cref="Microsoft.UI.Xaml.Controls.Flyout"/>-typed view
	/// surfaces on each fresh flyout instance and does not disturb the underlying Frame's navigation.
	/// <see cref="Uno.Extensions.Navigation.Navigators.FlyoutNavigator"/> presents the flyout via
	/// <c>ShowAt(placementTarget)</c> — it does not push onto the Frame's back stack and does not
	/// replace the Frame's current page (<c>FlyoutNavigator.ExecuteRequestAsync</c> returns
	/// <c>route with { Path = null }</c> for non-injected flyout types, so the Frame's route is
	/// untouched). The test: navigates to <see cref="HotReloadPageOne"/>, shows the flyout, applies
	/// an HR edit to the flyout's target, re-shows the flyout, and asserts throughout that the
	/// Frame's route stays pinned to the underlying page and that the underlying page instance/value
	/// is never mutated.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ShowFlyoutAfterUpdate_Then_FlyoutReflectsUpdateAndFrameStaysStable(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			registerViewsAndRoutes: (views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadFlyoutView>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("HotReloadFlyoutView", View: views.FindByView<HotReloadFlyoutView>()),
					}));
			},
			initialRoute: "HotReloadPageOne",
			ct);

		var page1 = ResolveCurrentPage<HotReloadPageOne>(app.NavigationRoot);
		page1.Should().NotBeNull("Frame should have navigated to HotReloadPageOne");
		page1!.DisplayedValue.Should().Be("original");

		// Pre-HR: show the flyout. The "!" qualifier (auto-detection via IsDialogViewType —
		// HotReloadFlyoutView subclasses Flyout) routes the request through FlyoutNavigator.
		// DisplayFlyout builds a fresh HotReloadFlyoutView via CreateInstance and calls ShowAt
		// using Region.View (or the window's content) as the placement target. The Frame is
		// untouched: ExecuteRequestAsync returns `route with { Path = null }` for non-injected
		// flyout types.
		await app.FrameNavigator.NavigateRouteAsync(this, "!HotReloadFlyoutView");

		var flyoutBefore = await WaitForFlyoutAsync(TimeSpan.FromSeconds(30), ct);
		flyoutBefore.DisplayedValue.Should().Be("original",
			"pre-HR flyout ctor should capture the unchanged method body");
		app.FrameNavigator.Route?.Base.Should().Be("HotReloadPageOne",
			"showing a flyout must not replace the Frame's underlying page");

		flyoutBefore.Hide();
		await WaitForFlyoutClosedAsync(TimeSpan.FromSeconds(10), ct);

		ResolveCurrentPage<HotReloadPageOne>(app.NavigationRoot).Should().BeSameAs(page1,
			"closing the flyout must not remount or replace the underlying page instance");
		app.FrameNavigator.Route?.Base.Should().Be("HotReloadPageOne",
			"closing the flyout must leave the Frame's route untouched");

		// HR: flip the flyout's target. Disposal reverts the file on scope exit.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadFlyoutTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Post-HR: re-show the flyout. FlyoutNavigator constructs a fresh Flyout per navigation
		// (DisplayFlyout → CreateInstance), so the new ctor runs against the HR'd method body.
		await app.FrameNavigator.NavigateRouteAsync(this, "!HotReloadFlyoutView");

		var flyoutAfter = await WaitForFlyoutAsync(TimeSpan.FromSeconds(30), ct);
		flyoutAfter.Should().NotBeSameAs(flyoutBefore,
			"FlyoutNavigator constructs a fresh flyout per navigation — the HR delta is observed via the new instance");
		flyoutAfter.DisplayedValue.Should().Be("updated",
			"post-HR flyout ctor should read the updated method body");

		// Core claim: HR on the flyout's target does not bleed into the underlying page or the
		// Frame's navigation state.
		page1.DisplayedValue.Should().Be("original",
			"underlying page's DisplayedValue was captured pre-HR and must remain unchanged");
		app.FrameNavigator.Route?.Base.Should().Be("HotReloadPageOne",
			"post-HR flyout show must not replace the Frame's underlying page");

		flyoutAfter.Hide();
		await WaitForFlyoutClosedAsync(TimeSpan.FromSeconds(10), ct);

		ResolveCurrentPage<HotReloadPageOne>(app.NavigationRoot).Should().BeSameAs(page1,
			"closing the post-HR flyout must not remount the underlying page");
		app.FrameNavigator.Route?.Base.Should().Be("HotReloadPageOne",
			"Frame's Route must remain on HotReloadPageOne after the full flyout+HR cycle");
	}

	private static async Task<HotReloadFlyoutView> WaitForFlyoutAsync(TimeSpan timeout, CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (HotReloadFlyoutView.Current is { } flyout)
			{
				return flyout;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"Flyout (HotReloadFlyoutView.Current) did not appear within {timeout.TotalSeconds:F0}s.");
	}

	private static async Task WaitForFlyoutClosedAsync(TimeSpan timeout, CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (HotReloadFlyoutView.Current is null)
			{
				return;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"Flyout did not close within {timeout.TotalSeconds:F0}s.");
	}

	private static async Task<HotReloadModalDialog> WaitForModalAsync(TimeSpan timeout, CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (HotReloadModalDialog.Current is { } modal)
			{
				return modal;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"Modal dialog (HotReloadModalDialog.Current) did not appear within {timeout.TotalSeconds:F0}s.");
	}

	private static async Task WaitForModalClosedAsync(TimeSpan timeout, CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (HotReloadModalDialog.Current is null)
			{
				return;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"Modal dialog did not close within {timeout.TotalSeconds:F0}s.");
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
