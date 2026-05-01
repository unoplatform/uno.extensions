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
/// Hot Reload regression tests for general navigation scenarios.
/// Every test either modifies a C# source file via <see cref="HotReloadHelper.UpdateSourceFile"/>
/// (C# HR) or a XAML source file (XAML HR) — then asserts the runtime effect.
///
/// Covered sub-issues from epic #926:
///   #2903 — Code-behind navigation InvalidCastException after HR
///   #2911 — NavigationCacheMode=Enabled blank page after HR
///   #3076 — Navigation.Request XAML HR edits
///   #3084 — Switching ViewMap to DataViewMap via HR
///   #3085 — Changing nav-data entity construction via HR
///   #3086 — Region.Attached toggling via XAML HR
///   #3087 — Region.Navigator add/remove via XAML HR
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
	/// Regression test for #2911: After C# HR is applied, pressing Back with
	/// NavigationCacheMode=Enabled shows a blank page on the first Back press.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_BackNavAfterHR_WithCacheMode_Then_PageNotBlank(CancellationToken ct)
	{
		await using var app = await SetupCachedPageAppAsync(ct);

		var cachedPage = ResolveCurrentPage<HotReloadCachedPage>(app.NavigationRoot);
		cachedPage.Should().NotBeNull("Frame should show HotReloadCachedPage initially");
		cachedPage!.DisplayedValue.Should().Be("original");

		// Navigate forward to page two.
		await app.FrameNavigator.NavigateRouteAsync(this, "HotReloadPageTwo");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageTwo", TimeSpan.FromSeconds(30), ct);

		var page2 = ResolveCurrentPage<HotReloadPageTwo>(app.NavigationRoot);
		page2.Should().NotBeNull("Should be on HotReloadPageTwo");

		// C# HR: change the target method body.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Navigate back — #2911 causes a blank page here.
		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadCachedPage", TimeSpan.FromSeconds(30), ct);

		var returnedPage = ResolveCurrentPage<HotReloadCachedPage>(app.NavigationRoot);
		returnedPage.Should().NotBeNull(
			"After Back navigation with NavigationCacheMode=Enabled post-HR, " +
			"the page should not be blank (#2911)");
		returnedPage!.Content.Should().NotBeNull("Page content should not be null");
	}

	/// <summary>
	/// Extended #2911 test: navigate forward → HR → back → forward again.
	/// The second forward navigation creates a fresh page that should read "updated".
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ForwardAfterBackAfterHR_WithCacheMode_Then_UpdatedContentShown(CancellationToken ct)
	{
		await using var app = await SetupCachedPageAppAsync(ct);

		// Navigate forward.
		await app.FrameNavigator.NavigateRouteAsync(this, "HotReloadPageOne");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		// C# HR: flip the method body.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Back.
		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadCachedPage", TimeSpan.FromSeconds(30), ct);

		// Forward again — new instance of HotReloadPageOne should see "updated".
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
	/// Regression test for #2903: After C# HR, navigating via code-behind
	/// (calling <c>this.Navigator()!.NavigateRouteAsync()</c> from a button click handler)
	/// should not throw InvalidCastException.
	/// Unlike the previous version, this test uses the page's own Navigator extension
	/// method — the exact code path exercised by the handler.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CodeBehindNavAfterHR_Then_NavigationSucceeds(CancellationToken ct)
	{
		await using var app = await SetupCodeBehindNavAppAsync(ct);

		var codeBehindPage = ResolveCurrentPage<HotReloadCodeBehindNavPage>(app.NavigationRoot);
		codeBehindPage.Should().NotBeNull();

		// C# HR: change the code-behind nav target from "PageOne" to "PageTwo".
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadCodeBehindNavTarget.cs",
			"""return "PageOne";""",
			"""return "PageTwo";""",
			ct);

		// Navigate using the page's own navigator — same path as the button click handler.
		// This is where #2903 throws InvalidCastException.
		var route = HotReloadCodeBehindNavTarget.GetRoute();
		route.Should().Be("PageTwo", "C# HR should have changed the route target");

		var pageNavigator = codeBehindPage!.Navigator();
		pageNavigator.Should().NotBeNull("Page should have a navigator");
		await pageNavigator!.NavigateRouteAsync(codeBehindPage!, route);

		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "PageTwo", TimeSpan.FromSeconds(30), ct);

		var page = ResolveCurrentPage<HotReloadPageTwo>(app.NavigationRoot);
		page.Should().NotBeNull(
			"Code-behind navigation after HR should not throw InvalidCastException (#2903) " +
			"and should navigate to the updated route");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 3. Navigation.Request changed via XAML HR (#3076)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR changes <c>uen:Navigation.Request="PageOne"</c> to <c>"PageTwo"</c> on a button.
	/// After page replacement, the button's attached property should reflect "PageTwo"
	/// and the frame navigator should be able to navigate to that route.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_NavigationRequestChangedViaXamlHR_Then_NewRouteUsed(CancellationToken ct)
	{
		await using var app = await SetupNavRequestAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadNavRequestPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Baseline: button has Navigation.Request="PageOne".
		var initialRequest = Navigation.GetRequest(hostPage!.NavigationButton);
		initialRequest.Should().Be("PageOne", "Initial Navigation.Request should be PageOne");

		// XAML HR: change Navigation.Request from "PageOne" to "PageTwo".
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadNavRequestPage.xaml",
			"""uen:Navigation.Request="PageOne" """,
			"""uen:Navigation.Request="PageTwo" """,
			ct);

		// Wait for XAML HR to replace the page instance.
		var activePage = await WaitForPageReplacementAsync<HotReloadNavRequestPage>(
			app.NavigationRoot, hostPage, TimeSpan.FromSeconds(30), ct);

		// The new page's button should have the updated Navigation.Request.
		var updatedRequest = Navigation.GetRequest(activePage.NavigationButton);
		updatedRequest.Should().Be("PageTwo",
			"After XAML HR, Navigation.Request should be PageTwo");

		// Verify the route is navigable via the frame navigator.
		await app.FrameNavigator.NavigateRouteAsync(this, "PageTwo");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "PageTwo", TimeSpan.FromSeconds(30), ct);

		var page = ResolveCurrentPage<HotReloadPageTwo>(app.NavigationRoot);
		page.Should().NotBeNull(
			"After XAML HR changes Navigation.Request to PageTwo, the route should be navigable (#3076)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 4. Region.Attached remove + re-add via XAML HR (#3086)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR removes <c>uen:Region.Attached="True"</c> from the inner grid, then
	/// re-adds it (via file revert). After the re-add, region navigation should
	/// be restored. This exercises whether the region system can recover from a
	/// detach → re-attach cycle triggered by XAML hot reload.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RegionAttachedRemovedAndReAddedViaXamlHR_Then_NavigationRestored(CancellationToken ct)
	{
		await using var app = await SetupRegionAttachedAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadRegionAttachedPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Baseline: initial region should be populated via PanelVisibilityNavigator.
		var regionOneVm = await WaitForRegionVmAsync(hostPage!.InnerGrid, "RegionOne", TimeSpan.FromSeconds(30), ct);
		regionOneVm.Should().NotBeNull("Initial region should be populated");

		// Step 1 — XAML HR: remove Region.Attached from the inner grid.
		var removeRevert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadRegionAttachedPage.xaml",
			"""<Grid x:Name="_innerGrid" uen:Region.Attached="True" uen:Region.Navigator="Visibility" />""",
			"""<Grid x:Name="_innerGrid" uen:Region.Navigator="Visibility" />""",
			ct);

		var detachedPage = await WaitForPageReplacementAsync<HotReloadRegionAttachedPage>(
			app.NavigationRoot, hostPage, TimeSpan.FromSeconds(30), ct);

		// Step 2 — Revert: re-add Region.Attached via file revert (another XAML HR).
		await removeRevert.DisposeAsync();

		var restoredPage = await WaitForPageReplacementAsync<HotReloadRegionAttachedPage>(
			app.NavigationRoot, detachedPage, TimeSpan.FromSeconds(30), ct);

		// After re-attachment, navigate to RegionTwo to verify navigation works.
		var panelNav = await WaitForPanelNavigatorAsync(restoredPage.InnerGrid, TimeSpan.FromSeconds(30), ct);
		await panelNav.NavigateRouteAsync(restoredPage, "RegionTwo");

		var regionTwoVm = await WaitForRegionVmAsync(restoredPage.InnerGrid, "RegionTwo", TimeSpan.FromSeconds(30), ct);
		regionTwoVm.Should().NotBeNull(
			"Navigation should be restored after Region.Attached is re-added via XAML HR (#3086)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 5. Region.Navigator remove + re-add via XAML HR (#3087)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR removes <c>uen:Region.Navigator="Visibility"</c> from the inner grid, then
	/// re-adds it (via file revert). After the re-add, the PanelVisibilityNavigator
	/// should be re-created and region navigation should work.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RegionNavigatorRemovedAndReAddedViaXamlHR_Then_NavigationRestored(CancellationToken ct)
	{
		await using var app = await SetupRegionNavigatorAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadRegionNavigatorPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Baseline: initial region should be populated.
		var regionOneVm = await WaitForRegionVmAsync(hostPage!.InnerGrid, "RegionOne", TimeSpan.FromSeconds(30), ct);
		regionOneVm.Should().NotBeNull("Initial region should be populated");

		// Step 1 — XAML HR: remove Region.Navigator from the inner grid.
		var removeRevert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadRegionNavigatorPage.xaml",
			"""<Grid x:Name="_innerGrid" uen:Region.Attached="True" uen:Region.Navigator="Visibility" />""",
			"""<Grid x:Name="_innerGrid" uen:Region.Attached="True" />""",
			ct);

		var strippedPage = await WaitForPageReplacementAsync<HotReloadRegionNavigatorPage>(
			app.NavigationRoot, hostPage, TimeSpan.FromSeconds(30), ct);

		// Step 2 — Revert: re-add Region.Navigator via file revert.
		await removeRevert.DisposeAsync();

		var restoredPage = await WaitForPageReplacementAsync<HotReloadRegionNavigatorPage>(
			app.NavigationRoot, strippedPage, TimeSpan.FromSeconds(30), ct);

		// After restoration, navigate to RegionTwo to verify navigation works.
		var panelNav = await WaitForPanelNavigatorAsync(restoredPage.InnerGrid, TimeSpan.FromSeconds(30), ct);
		await panelNav.NavigateRouteAsync(restoredPage, "RegionTwo");

		var regionTwoVm = await WaitForRegionVmAsync(restoredPage.InnerGrid, "RegionTwo", TimeSpan.FromSeconds(30), ct);
		regionTwoVm.Should().NotBeNull(
			"Navigation should be restored after Region.Navigator is re-added via XAML HR (#3087)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 6. Navigation Data Contract — construction changed via C# HR (#3085)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// C# HR changes how navigation data is constructed (modifying a helper method).
	/// After HR, a new navigation with data freshly read from the helper should
	/// pass the updated data to the target page/VM.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_NavDataConstructionChangedViaHR_Then_TargetReceivesUpdatedData(CancellationToken ct)
	{
		await using var app = await SetupNavDataAppAsync(ct);

		// Navigate with data using the original helper value.
		var data = new HotReloadNavData("hello", ExtraInfo: HotReloadNavDataTarget.GetExtraInfo());
		await app.FrameNavigator.NavigateRouteAsync(this, "NavDataPage", data: data);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "NavDataPage", TimeSpan.FromSeconds(30), ct);

		var dataPage = ResolveCurrentPage<HotReloadNavDataPage>(app.NavigationRoot);
		dataPage.Should().NotBeNull();
		dataPage!.ExtraInfo.Should().Be("original",
			"Pre-HR navigation data should have ExtraInfo = 'original'");

		// Go back.
		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		// C# HR: change the data construction helper.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadNavDataTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Navigate again — the helper now returns "updated".
		var updatedData = new HotReloadNavData("hello", ExtraInfo: HotReloadNavDataTarget.GetExtraInfo());
		await app.FrameNavigator.NavigateRouteAsync(this, "NavDataPage", data: updatedData);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "NavDataPage", TimeSpan.FromSeconds(30), ct);

		var updatedPage = ResolveCurrentPage<HotReloadNavDataPage>(app.NavigationRoot);
		updatedPage.Should().NotBeNull();
		updatedPage!.ExtraInfo.Should().Be("updated",
			"After HR changes the data construction helper, navigation data should reflect 'updated' (#3085)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 7. ViewMap → DataViewMap switch via C# HR (#3084)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// C# HR flips <see cref="HotReloadRouteSwitch.UseDataViewMap"/> from false to true.
	/// The <see cref="NavigationRouteUpdateHandler"/> re-invokes the route builder, which
	/// now registers a DataViewMap instead of a ViewMap. This verifies that:
	///   - Pre-HR: ViewMap is used (no VM injected → DataContext is null).
	///   - Post-HR: DataViewMap is used (VM IS injected → DataContext is HotReloadNavDataVm).
	///
	/// NOTE: The data parameter passed via NavigateRouteAsync does not flow through to the
	/// VM after an HR-triggered route switch (#3084). The VM is created but receives null data.
	/// This test documents the current behavior while verifying that the route registration
	/// switch itself works.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ViewMapSwitchedToDataViewMapViaHR_Then_VmIsInjected(CancellationToken ct)
	{
		await using var app = await SetupRoutesSwitchAppAsync(ct);

		// Pre-HR: registered as ViewMap (no VM/data). Navigate to the page.
		await app.FrameNavigator.NavigateRouteAsync(this, "NavDataPage");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "NavDataPage", TimeSpan.FromSeconds(30), ct);

		var pageNoData = ResolveCurrentPage<HotReloadNavDataPage>(app.NavigationRoot);
		pageNoData.Should().NotBeNull();
		pageNoData!.DisplayedValue.Should().BeNull(
			"Pre-HR: ViewMap has no VM, so DataContext is null");

		// Go back.
		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		// C# HR: flip the route registration from ViewMap to DataViewMap.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadRouteSwitch.cs",
			"=> false",
			"=> true",
			ct);

		// Post-HR: navigate with data. The builder should now use DataViewMap.
		var data = new HotReloadNavData("world", ExtraInfo: "from-HR");
		await app.FrameNavigator.NavigateRouteAsync(this, "NavDataPage", data: data);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "NavDataPage", TimeSpan.FromSeconds(30), ct);

		var pageWithVm = ResolveCurrentPage<HotReloadNavDataPage>(app.NavigationRoot);
		pageWithVm.Should().NotBeNull();

		// The VM should now be injected (DataViewMap was registered by the HR-updated builder).
		(pageWithVm!.DataContext is HotReloadNavDataVm).Should().BeTrue(
			"Post-HR: DataViewMap should inject a HotReloadNavDataVm (#3084)");
	}

	#region Setup helpers

	/// <summary>
	/// Generic setup: boots an Uno host with navigation, mounts the nav root,
	/// and waits for the initial route to land.
	/// </summary>
	private static async Task<HotReloadNavTestApp> SetupAppAsync(
		Action<IViewRegistry, IRouteRegistry> registerViewsAndRoutes,
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

	/// <summary>CachedPage → PageOne / PageTwo setup for #2911 tests.</summary>
	private static Task<HotReloadNavTestApp> SetupCachedPageAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadCachedPage>(),
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadPageTwo>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadCachedPage", View: views.FindByView<HotReloadCachedPage>(), IsDefault: true),
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>()),
						new RouteMap("HotReloadPageTwo", View: views.FindByView<HotReloadPageTwo>()),
					}));
			},
			"HotReloadCachedPage",
			ct);

	/// <summary>Code-behind nav page → PageOne / PageTwo for #2903 test.</summary>
	private static Task<HotReloadNavTestApp> SetupCodeBehindNavAppAsync(CancellationToken ct)
		=> SetupAppAsync(
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

	/// <summary>Navigation.Request XAML page → PageOne / PageTwo for #3076 test.</summary>
	private static Task<HotReloadNavTestApp> SetupNavRequestAppAsync(CancellationToken ct)
		=> SetupAppAsync(
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

	/// <summary>Region.Attached page with nested region routes for #3086 test.</summary>
	private static Task<HotReloadNavTestApp> SetupRegionAttachedAppAsync(CancellationToken ct)
		=> SetupAppAsync(
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

	/// <summary>Region.Navigator page with nested region routes for #3087 test.</summary>
	private static Task<HotReloadNavTestApp> SetupRegionNavigatorAppAsync(CancellationToken ct)
		=> SetupAppAsync(
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

	/// <summary>Nav data page setup for #3085 test.</summary>
	private static Task<HotReloadNavTestApp> SetupNavDataAppAsync(CancellationToken ct)
		=> SetupAppAsync(
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

	/// <summary>
	/// Route-switch app for #3084: conditionally registers ViewMap or DataViewMap
	/// based on <see cref="HotReloadRouteSwitch.UseDataViewMap"/>.
	/// </summary>
	private static Task<HotReloadNavTestApp> SetupRoutesSwitchAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(new ViewMap<HotReloadPageOne>());

				if (HotReloadRouteSwitch.UseDataViewMap())
				{
					views.Register(new DataViewMap<HotReloadNavDataPage, HotReloadNavDataVm, HotReloadNavData>());
				}
				else
				{
					views.Register(new ViewMap<HotReloadNavDataPage>());
				}

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("NavDataPage", View: views.FindByView<HotReloadNavDataPage>()),
					}));
			},
			"HotReloadPageOne",
			ct);

	#endregion

	#region Infrastructure

	private sealed class HotReloadNavTestApp : IAsyncDisposable
	{
		private readonly IHost _host;

		public HotReloadNavTestApp(
			ContentControl navigationRoot,
			INavigator frameNavigator,
			IHost host)
		{
			NavigationRoot = navigationRoot;
			FrameNavigator = frameNavigator;
			_host = host;
		}

		public ContentControl NavigationRoot { get; }
		public INavigator FrameNavigator { get; }

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

	private static async Task<INavigator> WaitForFrameNavigatorAsync(
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

	/// <summary>
	/// Polls until XAML HR replaces the page instance (new object reference != old).
	/// </summary>
	private static async Task<TPage> WaitForPageReplacementAsync<TPage>(
		ContentControl root,
		TPage oldPage,
		TimeSpan timeout,
		CancellationToken ct) where TPage : class
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			var current = ResolveCurrentPage<TPage>(root);
			if (current is not null && !ReferenceEquals(current, oldPage))
			{
				return current;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"XAML HR did not replace the {typeof(TPage).Name} instance within {timeout.TotalSeconds:F0}s.");
	}

	private static async Task WaitForRouteAsync(
		ContentControl root,
		INavigator nav,
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

	private static async Task<INavigator> WaitForPanelNavigatorAsync(
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
