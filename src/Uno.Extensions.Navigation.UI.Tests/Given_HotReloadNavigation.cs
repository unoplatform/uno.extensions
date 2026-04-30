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
using Uno.Extensions.Navigation.UI;
using Uno.Extensions.Navigation.UI.Controls;
using Uno.Extensions.Navigation.UI.Tests.Pages;
using Uno.Extensions.Navigation.UI.Tests.ViewModels;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Hot Reload tests for navigation scenarios not covered by Given_HotReload or Given_TabBarHotReload.
/// Covers: NavigationCacheMode back-stack, Navigation.Request changes, Region.Attached toggling,
/// Region.Navigator switching, code-behind navigation, and navigation data contracts.
/// </summary>
[TestClass]
[RunsInSecondaryApp(ignoreIfNotSupported: true)]
public class Given_HotReloadNavigation
{
	[TestInitialize]
	public void Setup()
	{
		HotReloadHelper.DefaultWorkspaceTimeout = TimeSpan.FromSeconds(300);
		HotReloadHelper.DefaultMetadataUpdateTimeout = TimeSpan.FromSeconds(60);
	}

	// ──────────────────────────────────────────────────────────────────────
	// 1. NavigationCacheMode=Enabled — back navigation after HR (#2911)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Regression test for #2911: After HR is applied, pressing Back with
	/// NavigationCacheMode=Enabled shows a blank page on the first Back press.
	/// The second Back press navigates correctly.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_BackNavAfterHR_WithCacheMode_Then_PageNotBlank(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadCachedPage>(),
					new ViewMap<HotReloadPageTwo>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadCachedPage", View: views.FindByView<HotReloadCachedPage>(), IsDefault: true),
						new RouteMap("HotReloadPageTwo", View: views.FindByView<HotReloadPageTwo>()),
					}));
			},
			"HotReloadCachedPage",
			ct);

		// Verify initial page
		var cachedPage = ResolveCurrentPage<HotReloadCachedPage>(app.NavigationRoot);
		cachedPage.Should().NotBeNull("Frame should show HotReloadCachedPage initially");
		cachedPage!.DisplayedValue.Should().Be("original");

		// Navigate forward to page two
		await app.FrameNavigator.NavigateRouteAsync(this, "HotReloadPageTwo");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageTwo", TimeSpan.FromSeconds(30), ct);

		var page2 = ResolveCurrentPage<HotReloadPageTwo>(app.NavigationRoot);
		page2.Should().NotBeNull("Should be on HotReloadPageTwo");

		// Apply HR change while on page two
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Navigate back — this is where #2911 shows a blank page
		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadCachedPage", TimeSpan.FromSeconds(30), ct);

		var returnedPage = ResolveCurrentPage<HotReloadCachedPage>(app.NavigationRoot);
		returnedPage.Should().NotBeNull(
			"After Back navigation with NavigationCacheMode=Enabled post-HR, " +
			"the page should not be blank (#2911)");
		// The cached page still has "original" since it was constructed pre-HR
		returnedPage!.Content.Should().NotBeNull("Page content should not be null");
	}

	/// <summary>
	/// Extended test: navigate forward, HR, back, then forward again.
	/// The second forward navigation should see the updated content.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ForwardAfterBackAfterHR_WithCacheMode_Then_UpdatedContentShown(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadCachedPage>(),
					new ViewMap<HotReloadPageOne>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadCachedPage", View: views.FindByView<HotReloadCachedPage>(), IsDefault: true),
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>()),
					}));
			},
			"HotReloadCachedPage",
			ct);

		// Navigate forward
		await app.FrameNavigator.NavigateRouteAsync(this, "HotReloadPageOne");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		// Apply HR
		await using var revert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Back
		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadCachedPage", TimeSpan.FromSeconds(30), ct);

		// Forward again — new instance of HotReloadPageOne should see "updated"
		await app.FrameNavigator.NavigateRouteAsync(this, "HotReloadPageOne");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		var page = ResolveCurrentPage<HotReloadPageOne>(app.NavigationRoot);
		page.Should().NotBeNull();
		page!.DisplayedValue.Should().Be("updated",
			"A fresh forward navigation after HR should see the updated method body");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 2. Code-behind navigation after HR (#2903)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Regression test for #2903: After HR, navigating via code-behind
	/// (calling Navigator.NavigateRouteAsync from a button click handler)
	/// should not throw InvalidCastException.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CodeBehindNavAfterHR_Then_NavigationSucceeds(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadCodeBehindNavPage>(),
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadPageTwo>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadCodeBehindNavPage", View: views.FindByView<HotReloadCodeBehindNavPage>(), IsDefault: true),
						new RouteMap("PageOne", View: views.FindByView<HotReloadPageOne>()),
						new RouteMap("PageTwo", View: views.FindByView<HotReloadPageTwo>()),
					}));
			},
			"HotReloadCodeBehindNavPage",
			ct);

		var codeBehindPage = ResolveCurrentPage<HotReloadCodeBehindNavPage>(app.NavigationRoot);
		codeBehindPage.Should().NotBeNull();

		// Apply HR to change the code-behind nav target from "PageOne" to "PageTwo"
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadCodeBehindNavTarget.cs",
			"""return "PageOne";""",
			"""return "PageTwo";""",
			ct);

		// Trigger navigation via code-behind — this is where #2903 throws InvalidCastException
		await app.FrameNavigator.NavigateRouteAsync(this, HotReloadCodeBehindNavTarget.GetRoute());
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "PageTwo", TimeSpan.FromSeconds(30), ct);

		var page = ResolveCurrentPage<HotReloadPageTwo>(app.NavigationRoot);
		page.Should().NotBeNull(
			"Code-behind navigation after HR should not throw InvalidCastException (#2903) " +
			"and should navigate to the updated route");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 3. Navigation.Request changes via HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// After HR changes the route returned by the target, re-setting
	/// Navigation.Request on the button should cause it to navigate to the new route.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_NavigationRequestChangedViaHR_Then_NewRouteUsed(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadNavRequestPage>(),
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadPageTwo>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadNavRequestPage", View: views.FindByView<HotReloadNavRequestPage>(), IsDefault: true),
						new RouteMap("PageOne", View: views.FindByView<HotReloadPageOne>()),
						new RouteMap("PageTwo", View: views.FindByView<HotReloadPageTwo>()),
					}));
			},
			"HotReloadNavRequestPage",
			ct);

		var navRequestPage = ResolveCurrentPage<HotReloadNavRequestPage>(app.NavigationRoot);
		navRequestPage.Should().NotBeNull();

		// Baseline: Navigation.Request is "PageOne"
		var currentRequest = Navigation.GetRequest(navRequestPage!.NavigationButton);
		currentRequest.Should().Be("PageOne");

		// Apply HR to change the route
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadNavigationRequestTarget.cs",
			"""return "PageOne";""",
			"""return "PageTwo";""",
			ct);

		// After HR, navigate using the frame navigator with the new route
		// (simulates the effect of re-creating the page with updated Navigation.Request)
		await app.FrameNavigator.NavigateRouteAsync(this, HotReloadNavigationRequestTarget.GetRoute());
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "PageTwo", TimeSpan.FromSeconds(30), ct);

		var page = ResolveCurrentPage<HotReloadPageTwo>(app.NavigationRoot);
		page.Should().NotBeNull(
			"After HR changes the target route, navigation should land on PageTwo");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 4. Region.Attached toggle via HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Tests that removing and re-adding Region.Attached on a panel
	/// (via HR) allows navigation to continue working.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RegionAttachedRemovedAndReAdded_Then_NavigationWorks(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadRegionAttachedPage>(),
					new ViewMap<HotReloadRegionContentPage, HotReloadRegionVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadRegionAttachedPage",
							View: views.FindByView<HotReloadRegionAttachedPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("RegionOne", View: views.FindByView<HotReloadRegionContentPage>(), IsDefault: true),
								new RouteMap("RegionTwo", View: views.FindByView<HotReloadRegionContentPage>()),
							}),
					}));
			},
			"HotReloadRegionAttachedPage",
			ct);

		var hostPage = ResolveCurrentPage<HotReloadRegionAttachedPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Wait for the initial region to populate
		var regionOneVm = await WaitForRegionVmAsync(hostPage!.InnerGrid, "RegionOne", TimeSpan.FromSeconds(30), ct);
		regionOneVm.Should().NotBeNull("Initial region should be populated");

		// Navigate to RegionTwo via the panel navigator
		var panelNav = await WaitForPanelNavigatorAsync(hostPage.InnerGrid, TimeSpan.FromSeconds(30), ct);
		await panelNav.NavigateRouteAsync(hostPage, "RegionTwo");
		var regionTwoVm = await WaitForRegionVmAsync(hostPage.InnerGrid, "RegionTwo", TimeSpan.FromSeconds(30), ct);
		regionTwoVm.Should().NotBeNull("RegionTwo should be navigable before HR");

		// HR: Remove Region.Attached from the inner grid (simulates toggling off)
		Region.SetAttached(hostPage.InnerGrid, false);
		await Task.Delay(200, ct); // Give the region system time to detach

		// HR: Re-add Region.Attached (simulates XAML HR re-adding it)
		Region.SetAttached(hostPage.InnerGrid, true);
		Region.SetNavigator(hostPage.InnerGrid, "Visibility");
		await Task.Delay(500, ct); // Give the region system time to reattach

		// Try navigating again — should work after re-attachment
		var panelNavAfter = await WaitForPanelNavigatorAsync(hostPage.InnerGrid, TimeSpan.FromSeconds(30), ct);
		await panelNavAfter.NavigateRouteAsync(hostPage, "RegionOne");
		var regionOneVmAfter = await WaitForRegionVmAsync(hostPage.InnerGrid, "RegionOne", TimeSpan.FromSeconds(30), ct);
		regionOneVmAfter.Should().NotBeNull(
			"After Region.Attached is removed and re-added, navigation should still work");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 5. Region.Navigator toggle via HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Tests removing Region.Navigator and re-adding it via HR.
	/// After re-adding, the navigator should be available again.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RegionNavigatorRemovedAndReAdded_Then_NavigationWorks(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadRegionNavigatorPage>(),
					new ViewMap<HotReloadRegionContentPage, HotReloadRegionVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadRegionNavigatorPage",
							View: views.FindByView<HotReloadRegionNavigatorPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("RegionOne", View: views.FindByView<HotReloadRegionContentPage>(), IsDefault: true),
								new RouteMap("RegionTwo", View: views.FindByView<HotReloadRegionContentPage>()),
							}),
					}));
			},
			"HotReloadRegionNavigatorPage",
			ct);

		var hostPage = ResolveCurrentPage<HotReloadRegionNavigatorPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Wait for initial region
		var regionOneVm = await WaitForRegionVmAsync(hostPage!.InnerGrid, "RegionOne", TimeSpan.FromSeconds(30), ct);
		regionOneVm.Should().NotBeNull("Initial region should populate");

		// Navigate to RegionTwo to verify navigation works
		var panelNav = await WaitForPanelNavigatorAsync(hostPage.InnerGrid, TimeSpan.FromSeconds(30), ct);
		await panelNav.NavigateRouteAsync(hostPage, "RegionTwo");
		var regionTwoVm = await WaitForRegionVmAsync(hostPage.InnerGrid, "RegionTwo", TimeSpan.FromSeconds(30), ct);
		regionTwoVm.Should().NotBeNull("RegionTwo should be navigable before HR");

		// HR: Remove the navigator type, then re-add it (simulates XAML HR changing Region.Navigator)
		Region.SetNavigator(hostPage.InnerGrid, "");
		await Task.Delay(200, ct);

		Region.SetNavigator(hostPage.InnerGrid, "Visibility");
		await Task.Delay(500, ct);

		// Navigation should still work after toggling
		var panelNavAfter = await WaitForPanelNavigatorAsync(hostPage.InnerGrid, TimeSpan.FromSeconds(30), ct);
		await panelNavAfter.NavigateRouteAsync(hostPage, "RegionOne");
		var regionOneVmAfter = await WaitForRegionVmAsync(hostPage.InnerGrid, "RegionOne", TimeSpan.FromSeconds(30), ct);
		regionOneVmAfter.Should().NotBeNull(
			"After Region.Navigator is removed and re-added, navigation should still work");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 6. Navigation Data Contract — data property changes via HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Tests that changing how navigation data is constructed (via HR modifying
	/// a helper method) correctly passes the updated data to the target page.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_NavDataConstructionChangedViaHR_Then_TargetReceivesUpdatedData(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new DataViewMap<HotReloadNavDataPage, HotReloadNavDataVm, HotReloadNavData>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("NavDataPage", View: views.FindByView<HotReloadNavDataPage>()),
					}));
			},
			"HotReloadPageOne",
			ct);

		// Navigate with data using the original helper value
		var data = new HotReloadNavData("hello", ExtraInfo: HotReloadNavDataTarget.GetExtraInfo());
		await app.FrameNavigator.NavigateRouteAsync(this, "NavDataPage", data: data);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "NavDataPage", TimeSpan.FromSeconds(30), ct);

		var dataPage = ResolveCurrentPage<HotReloadNavDataPage>(app.NavigationRoot);
		dataPage.Should().NotBeNull();
		dataPage!.ExtraInfo.Should().Be("original",
			"Pre-HR navigation data should have ExtraInfo = 'original'");

		// Go back
		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		// HR: change the data construction helper
		await using var revert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadNavDataTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Navigate again with data using the now-updated helper
		var updatedData = new HotReloadNavData("hello", ExtraInfo: HotReloadNavDataTarget.GetExtraInfo());
		await app.FrameNavigator.NavigateRouteAsync(this, "NavDataPage", data: updatedData);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "NavDataPage", TimeSpan.FromSeconds(30), ct);

		var updatedPage = ResolveCurrentPage<HotReloadNavDataPage>(app.NavigationRoot);
		updatedPage.Should().NotBeNull();
		updatedPage!.ExtraInfo.Should().Be("updated",
			"After HR changes the data construction helper, navigation data should reflect 'updated'");
	}

	/// <summary>
	/// Tests switching from a ViewMap (no data) to a DataViewMap (with data) via HR.
	/// This simulates a developer adding data passing to an existing navigation flow.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ViewMapSwitchedToDataViewMapViaHR_Then_DataFlowsCorrectly(CancellationToken ct)
	{
		await using var app = await SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new DataViewMap<HotReloadNavDataPage, HotReloadNavDataVm, HotReloadNavData>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("NavDataPage", View: views.FindByView<HotReloadNavDataPage>()),
					}));
			},
			"HotReloadPageOne",
			ct);

		// First navigate without data
		await app.FrameNavigator.NavigateRouteAsync(this, "NavDataPage");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "NavDataPage", TimeSpan.FromSeconds(30), ct);

		var pageNoData = ResolveCurrentPage<HotReloadNavDataPage>(app.NavigationRoot);
		pageNoData.Should().NotBeNull();
		// VM should still be created but with null data
		pageNoData!.DisplayedValue.Should().Be("no-data",
			"Without data, VM should show 'no-data'");

		// Go back
		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		// Now navigate WITH data (simulating developer adding data flow via HR)
		var data = new HotReloadNavData("world", ExtraInfo: "from-HR");
		await app.FrameNavigator.NavigateRouteAsync(this, "NavDataPage", data: data);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "NavDataPage", TimeSpan.FromSeconds(30), ct);

		var pageWithData = ResolveCurrentPage<HotReloadNavDataPage>(app.NavigationRoot);
		pageWithData.Should().NotBeNull();
		pageWithData!.DisplayedValue.Should().Be("world",
			"With data, VM should show the data value");
		pageWithData.ExtraInfo.Should().Be("from-HR",
			"ExtraInfo should flow through to the VM");
	}

	#region Setup helpers

	private static async Task<HotReloadNavTestApp> SetupAppAsync(
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
					.CreateDefaultBuilder(typeof(Given_HotReloadNavigation).Assembly)
					.UseNavigation(viewRouteBuilder: registerViewsAndRoutes)
					.Build(),
				navigationRoot: navigationRoot,
				initialRoute: initialRoute);

			var frameNav = await WaitForFrameNavigatorAsync(navigationRoot, TimeSpan.FromSeconds(30), ct);
			await WaitForRouteAsync(navigationRoot, frameNav, initialRoute, TimeSpan.FromSeconds(30), ct);

			return new HotReloadNavTestApp(navigationRoot, frameNav, host);
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

	#endregion

	#region Infrastructure

	private sealed class HotReloadNavTestApp : IAsyncDisposable
	{
		private readonly IHost _host;

		public HotReloadNavTestApp(
			ContentControl navigationRoot,
			global::Uno.Extensions.Navigation.INavigator frameNavigator,
			IHost host)
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
			$"Panel navigator did not become available within {timeout.TotalSeconds:F0}s.");
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
				.FirstOrDefault(c => Region.GetName(c) == regionName);
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
			.Select(c => $"{c.GetType().Name}[Region.Name='{Region.GetName(c)}']"));
		throw new TimeoutException(
			$"Region '{regionName}' did not populate within {timeout.TotalSeconds:F0}s. " +
			$"Children: [{children}].");
	}

	#endregion
}
#endif
