#if DEBUG // Hot-reload tests are only relevant in debug configuration
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
///   #3077 — Navigation.Data dynamic bindings via XAML HR
///   #3078 — Frame.Navigate from event handler after HR
///   #3083 — Modifying VM constructor body logic via HR
///   #3084 — Switching ViewMap to DataViewMap via HR
///   #3085 — Changing nav-data entity construction via HR
///   #3086 — Region.Attached toggling via XAML HR
///   #3087 — Region.Navigator add/remove via XAML HR
///   #3088 — Renaming a Region across TabBarItem + content region via XAML HR
///   #3072 — Adding/removing a RouteMap at runtime via C# HR
///   #3075 — Region.Name add/remove on NavigationView/TabBar via XAML HR
///   #2904 — Panel Region.Names swap via XAML HR (Visibility navigator)
/// </summary>
[TestClass]
[RunsInSecondaryApp(ignoreIfNotSupported: true)]
public class Given_NavigationHotReload
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

	// ──────────────────────────────────────────────────────────────────────
	// 8. Navigation.Data changed via XAML HR (#3077)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR changes <c>uen:Navigation.Data="OriginalData"</c> to <c>"UpdatedData"</c>
	/// on a button. After page replacement, the attached property should reflect
	/// the updated value.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_NavigationDataChangedViaXamlHR_Then_UpdatedDataReflected(CancellationToken ct)
	{
		await using var app = await SetupNavDataBindingAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadNavDataBindingPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Baseline: Navigation.Data should be "OriginalData".
		var initialData = hostPage!.NavigationButton.GetData();
		initialData.Should().Be("OriginalData",
			"Initial Navigation.Data should be 'OriginalData'");

		// XAML HR: change Navigation.Data from "OriginalData" to "UpdatedData".
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadNavDataBindingPage.xaml",
			"""uen:Navigation.Data="OriginalData" """,
			"""uen:Navigation.Data="UpdatedData" """,
			ct);

		// Wait for XAML HR to replace the page instance.
		var activePage = await WaitForPageReplacementAsync<HotReloadNavDataBindingPage>(
			app.NavigationRoot, hostPage, TimeSpan.FromSeconds(30), ct);

		// The new page's button should have the updated Navigation.Data.
		var updatedData = activePage.NavigationButton.GetData();
		updatedData.Should().Be("UpdatedData",
			"After XAML HR, Navigation.Data should be 'UpdatedData' (#3077)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 9. Frame.Navigate from event handler after HR (#3078)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// After C# HR changes the route used in a button click event handler,
	/// invoking the handler should navigate to the new route without errors.
	/// This tests the Frame.Navigate path triggered from an event handler.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_FrameNavigateFromEventHandlerAfterHR_Then_NewRouteUsed(CancellationToken ct)
	{
		await using var app = await SetupFrameNavAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadFrameNavPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Pre-HR: the event handler navigates to "PageOne".
		HotReloadFrameNavTarget.GetRoute().Should().Be("PageOne");

		// C# HR: change the handler's target route from "PageOne" to "PageTwo".
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadFrameNavTarget.cs",
			"""return "PageOne";""",
			"""return "PageTwo";""",
			ct);

		// Verify the target method was updated.
		HotReloadFrameNavTarget.GetRoute().Should().Be("PageTwo");

		// Navigate using the handler's route (same path as the button click).
		var pageNavigator = hostPage!.Navigator();
		pageNavigator.Should().NotBeNull();
		var route = HotReloadFrameNavTarget.GetRoute();
		await pageNavigator!.NavigateRouteAsync(hostPage!, route);

		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "PageTwo", TimeSpan.FromSeconds(30), ct);

		var page = ResolveCurrentPage<HotReloadPageTwo>(app.NavigationRoot);
		page.Should().NotBeNull(
			"Frame.Navigate from event handler after HR should navigate to the updated route (#3078)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 10. Modifying VM constructor body logic via HR (#3083)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// C# HR changes the computation logic called from a ViewModel constructor.
	/// After HR, navigating to a page that creates a new VM instance should
	/// see the updated computed value.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_VmCtorBodyChangedViaHR_Then_NewInstanceReflectsUpdate(CancellationToken ct)
	{
		await using var app = await SetupVmCtorAppAsync(ct);

		// Navigate to the VM page.
		await app.FrameNavigator.NavigateRouteAsync(this, "VmCtorPage");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "VmCtorPage", TimeSpan.FromSeconds(30), ct);

		var page = ResolveCurrentPage<HotReloadVmCtorPage>(app.NavigationRoot);
		page.Should().NotBeNull();
		page!.DisplayedValue.Should().Be("original-test",
			"Pre-HR: VM should compute 'original-test'");

		// Go back.
		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		// C# HR: change the VM constructor body logic.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadVmCtorTarget.cs",
			"""return $"original-{input}";""",
			"""return $"updated-{input}";""",
			ct);

		// Navigate again — new VM instance should use updated logic.
		await app.FrameNavigator.NavigateRouteAsync(this, "VmCtorPage");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "VmCtorPage", TimeSpan.FromSeconds(30), ct);

		var updatedPage = ResolveCurrentPage<HotReloadVmCtorPage>(app.NavigationRoot);
		updatedPage.Should().NotBeNull();
		updatedPage!.DisplayedValue.Should().Be("updated-test",
			"After HR changes VM ctor body logic, new instance should reflect 'updated-test' (#3083)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 11. Renaming a Region across TabBarItem + content region via XAML HR (#3088)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR renames a Region.Name on a TabBarItem from "AlphaRegion" to
	/// "GammaRegion". After the page replacement, navigation should resolve
	/// the new region name.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RegionRenamedViaXamlHR_Then_NavigationUsesNewName(CancellationToken ct)
	{
		await using var app = await SetupRegionRenameAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadRegionRenamePage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Baseline: AlphaRegion should be navigable.
		var panelNav = await WaitForPanelNavigatorAsync(hostPage!.ContentGrid, TimeSpan.FromSeconds(30), ct);
		panelNav.Route?.Base.Should().Be("AlphaRegion",
			"Initial default region should be AlphaRegion");

		// XAML HR: rename "AlphaRegion" to "GammaRegion" on the TabBarItem.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadRegionRenamePage.xaml",
			"""uen:Region.Name="AlphaRegion" """,
			"""uen:Region.Name="GammaRegion" """,
			ct);

		// Wait for page replacement.
		var renamedPage = await WaitForPageReplacementAsync<HotReloadRegionRenamePage>(
			app.NavigationRoot, hostPage, TimeSpan.FromSeconds(30), ct);

		// After rename, navigate to "GammaRegion" should work.
		var renamedPanelNav = await WaitForPanelNavigatorAsync(renamedPage.ContentGrid, TimeSpan.FromSeconds(30), ct);
		await renamedPanelNav.NavigateRouteAsync(this, "GammaRegion");
		await Task.Delay(500, ct); // Allow navigation to settle.

		renamedPanelNav.Route?.Base.Should().Be("GammaRegion",
			"After XAML HR renames the region, navigation should resolve the new name (#3088)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 12. IRouteNotifier handler edits via HR (#3089)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// C# HR changes the logic inside an IRouteNotifier.RouteChanged event handler
	/// (simulated via a static method call). After HR, subsequent route changes
	/// should invoke the updated handler logic.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RouteNotifierHandlerChangedViaHR_Then_UpdatedLogicExecutes(CancellationToken ct)
	{
		await using var app = await SetupRouteNotifierAppAsync(ct);

		// Get the IRouteNotifier from the host services.
		var notifier = app.Host.Services.GetRequiredService<IRouteNotifier>();
		notifier.Should().NotBeNull();

		// Track handler results.
		string? lastResult = null;
		notifier!.RouteChanged += (_, e) =>
		{
			var route = e.Navigator?.Route?.Base ?? "unknown";
			lastResult = HotReloadRouteNotifierTarget.ProcessRouteChange(route);
		};

		// Navigate to trigger RouteChanged.
		await app.FrameNavigator.NavigateRouteAsync(this, "PageOne");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "PageOne", TimeSpan.FromSeconds(30), ct);

		// The handler should have processed with "handled-" prefix.
		lastResult.Should().NotBeNull();
		lastResult.Should().StartWith("handled-",
			"Pre-HR: handler should use 'handled-' prefix");

		// C# HR: change the handler logic from "handled-" to "modified-".
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadRouteNotifierTarget.cs",
			"""return $"handled-{route}";""",
			"""return $"modified-{route}";""",
			ct);

		// Navigate again to trigger RouteChanged with updated handler.
		lastResult = null;
		await app.FrameNavigator.NavigateRouteAsync(this, "PageTwo");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "PageTwo", TimeSpan.FromSeconds(30), ct);

		lastResult.Should().NotBeNull();
		lastResult.Should().StartWith("modified-",
			"After HR changes the RouteNotifier handler logic, it should use 'modified-' prefix (#3089)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 13. Flyout / Dialog VM logic updated via HR (#3079)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// A flyout page (shown via the "!" qualifier) has a ViewModel whose
	/// constructor calls an HR target. After HR changes the target method body,
	/// re-showing the flyout creates a new VM instance with updated logic.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_FlyoutVmLogicChangedViaHR_Then_ReshownFlyoutReflectsUpdate(CancellationToken ct)
	{
		await using var app = await SetupFlyoutAppAsync(ct);

		// Reset static tracker.
		HotReloadFlyoutVm.LastLabel = null;

		// Show the flyout via "!" qualifier.
		await app.FrameNavigator.NavigateRouteAsync(this, "!FlyoutPage");
		await Task.Delay(500, ct); // Allow flyout to open and VM to construct.

		HotReloadFlyoutVm.LastLabel.Should().Be("flyout-v1",
			"Pre-HR: flyout VM should compute 'flyout-v1'");

		// Close the flyout (back/close navigation).
		await app.FrameNavigator.NavigateRouteAsync(this, "-");
		await Task.Delay(300, ct);

		// C# HR: change the label from "flyout-v1" to "flyout-v2".
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadFlyoutTarget.cs",
			"""=> "flyout-v1";""",
			"""=> "flyout-v2";""",
			ct);

		// Reset and re-show the flyout.
		HotReloadFlyoutVm.LastLabel = null;
		await app.FrameNavigator.NavigateRouteAsync(this, "!FlyoutPage");
		await Task.Delay(500, ct);

		HotReloadFlyoutVm.LastLabel.Should().Be("flyout-v2",
			"After HR changes the flyout target, re-shown flyout VM should compute 'flyout-v2' (#3079)");

		// Cleanup: close flyout.
		await app.FrameNavigator.NavigateRouteAsync(this, "-");
		await Task.Delay(200, ct);
	}

	// ──────────────────────────────────────────────────────────────────────
	// 14. Nav-data model new property populated after HR (#3080)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// A navigation data model has an optional property initially left null.
	/// After HR changes the data construction code to populate it, the VM
	/// on the target page should receive the full data including the new property.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_NavDataNewPropertyPopulatedAfterHR_Then_VmReceivesFullData(CancellationToken ct)
	{
		await using var app = await SetupNavDataNewPropAppAsync(ct);

		// Navigate with data where NewProperty is null (pre-HR).
		var data = new HotReloadNavDataWithNewProp("hello", NewProperty: HotReloadNavDataNewPropTarget.GetNewProperty());
		await app.FrameNavigator.NavigateRouteAsync(this, "NavDataNewPropPage", data: data);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "NavDataNewPropPage", TimeSpan.FromSeconds(30), ct);

		var page = ResolveCurrentPage<HotReloadNavDataNewPropPage>(app.NavigationRoot);
		page.Should().NotBeNull();
		var vm = page!.DataContext as HotReloadNavDataNewPropVm;
		vm.Should().NotBeNull();
		vm!.ReceivedData.Should().NotBeNull();
		vm.ReceivedData!.Value.Should().Be("hello");
		vm.ReceivedData.NewProperty.Should().BeNull(
			"Pre-HR: NewProperty should be null because the target returns null");

		// C# HR: change the target to return a value.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadNavDataNewPropTarget.cs",
			"""GetNewProperty() => null;""",
			"""GetNewProperty() => "added-via-hr";""",
			ct);

		// Navigate back and re-navigate with updated data.
		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		var updatedData = new HotReloadNavDataWithNewProp("hello", NewProperty: HotReloadNavDataNewPropTarget.GetNewProperty());
		await app.FrameNavigator.NavigateRouteAsync(this, "NavDataNewPropPage", data: updatedData);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "NavDataNewPropPage", TimeSpan.FromSeconds(30), ct);

		var updatedPage = ResolveCurrentPage<HotReloadNavDataNewPropPage>(app.NavigationRoot);
		updatedPage.Should().NotBeNull();
		var updatedVm = updatedPage!.DataContext as HotReloadNavDataNewPropVm;
		updatedVm.Should().NotBeNull();
		updatedVm!.ReceivedData.Should().NotBeNull();
		updatedVm.ReceivedData!.NewProperty.Should().Be("added-via-hr",
			"After HR populates the new property, VM should receive the full data (#3080)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 15. Optional VM constructor parameter used after HR (#3081)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// A ViewModel has an optional constructor parameter (ILogger) injected by DI.
	/// The constructor body initially ignores it. After HR changes the body to
	/// incorporate the optional parameter, the next VM instance reflects the update.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_VmOptionalCtorParamUsedAfterHR_Then_NewInstanceReflectsUpdate(CancellationToken ct)
	{
		await using var app = await SetupOptionalParamAppAsync(ct);

		// Navigate to the page with the VM.
		HotReloadOptionalParamVm.LastComputedValue = null;
		await app.FrameNavigator.NavigateRouteAsync(this, "OptionalParamPage");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "OptionalParamPage", TimeSpan.FromSeconds(30), ct);

		// Pre-HR: target ignores the optional param, returns just "base".
		HotReloadOptionalParamVm.LastComputedValue.Should().Be("base",
			"Pre-HR: ComputeWithOptional should return just 'base' (ignoring optional param)");

		// Go back.
		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		// C# HR: change target to use the optional param.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadOptionalParamTarget.cs",
			"return baseValue;",
			"""return $"{baseValue}+{optionalInfo}";""",
			ct);

		// Navigate again — new VM instance should use the updated logic.
		HotReloadOptionalParamVm.LastComputedValue = null;
		await app.FrameNavigator.NavigateRouteAsync(this, "OptionalParamPage");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "OptionalParamPage", TimeSpan.FromSeconds(30), ct);

		HotReloadOptionalParamVm.LastComputedValue.Should().Be("base+logger-present",
			"After HR, target should incorporate optional param. DI injects ILogger so value should be 'base+logger-present' (#3081)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 16. VM constructor parameter usage reordered via HR (#3082)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// A ViewModel with multiple DI-injected parameters has its constructor body
	/// changed via HR to combine the parameters in a different order.
	/// Verifies DI still resolves all parameters and the new logic applies.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_VmCtorParamOrderChangedViaHR_Then_NewInstanceReflectsUpdate(CancellationToken ct)
	{
		await using var app = await SetupParamOrderAppAsync(ct);

		// Navigate to the page with the VM.
		HotReloadParamOrderVm.LastResult = null;
		await app.FrameNavigator.NavigateRouteAsync(this, "ParamOrderPage");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "ParamOrderPage", TimeSpan.FromSeconds(30), ct);

		// Pre-HR: Combine(loggerInfo, navInfo) → "log-nav".
		HotReloadParamOrderVm.LastResult.Should().Be("log-nav",
			"Pre-HR: Combine should produce 'log-nav' (first=log, second=nav)");

		// Go back.
		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		// C# HR: reverse the parameter order in Combine call.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadParamOrderTarget.cs",
			"""return $"{first}-{second}";""",
			"""return $"{second}-{first}";""",
			ct);

		// Navigate again — new VM instance should use the updated order.
		HotReloadParamOrderVm.LastResult = null;
		await app.FrameNavigator.NavigateRouteAsync(this, "ParamOrderPage");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "ParamOrderPage", TimeSpan.FromSeconds(30), ct);

		HotReloadParamOrderVm.LastResult.Should().Be("nav-log",
			"After HR reverses parameter order in Combine, result should be 'nav-log' (#3082)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 17. Rude-edit resilience — navigation remains functional (#3073/#3074)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Documents that navigation remains fully functional even after an HR
	/// attempt that modifies a target file. Whether the edit is applied or
	/// rejected as "rude" by the runtime, subsequent navigation requests
	/// must not crash. This test verifies graceful degradation.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_HRAttemptedOnMethodWithSideEffects_Then_NavigationRemainsStable(CancellationToken ct)
	{
		await using var app = await SetupCachedPageAppAsync(ct);

		HotReloadRudeEditTarget.ResetCallCount();

		// Navigate successfully before any HR attempt.
		await app.FrameNavigator.NavigateRouteAsync(this, "HotReloadPageOne");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		// Call the target to establish baseline.
		var preValue = HotReloadRudeEditTarget.GetStableValue();
		preValue.Should().Be("stable", "Pre-HR: target should return 'stable'");

		// Attempt an HR edit that adds a field initializer (supported in .NET 9+)
		// and changes the method body. If rude, the old code persists.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadRudeEditTarget.cs",
			"""return "stable";""",
			"""return "modified";""",
			ct);

		// Navigate forward and backward multiple times to stress the nav pipeline.
		await app.FrameNavigator.NavigateRouteAsync(this, "HotReloadPageTwo");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageTwo", TimeSpan.FromSeconds(30), ct);

		await app.FrameNavigator.NavigateBackAsync(this);
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

		await app.FrameNavigator.NavigateRouteAsync(this, "HotReloadPageTwo");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "HotReloadPageTwo", TimeSpan.FromSeconds(30), ct);

		// The target should now return either "modified" (if HR was applied)
		// or "stable" (if the edit was rejected as rude). Both are acceptable.
		var postValue = HotReloadRudeEditTarget.GetStableValue();
		(postValue == "modified" || postValue == "stable").Should().BeTrue(
			"After HR attempt, target should return either 'modified' (applied) or 'stable' (rejected). " +
			$"Got: '{postValue}'. Navigation remained stable regardless (#3073/#3074)");

		// Key assertion: navigation didn't crash — we reached this point.
		// The call count proves the method was still invocable.
		HotReloadRudeEditTarget.GetCallCount().Should().BeGreaterThan(0,
			"Target method remained callable after HR attempt (#3073/#3074)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 18. Adding a RouteMap at runtime via C# HR (#3072)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// C# HR flips a flag that triggers InsertRoute on the resolver,
	/// making a previously-unregistered route explicitly navigable at runtime.
	/// This validates that InsertRoute integrates with the navigation pipeline (#3072).
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RouteMapAddedViaHR_Then_NewRouteNavigable(CancellationToken ct)
	{
		await using var app = await SetupRouteRegistrationAppAsync(ct);

		// C# HR: flip the flag to simulate a developer adding a new route.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadRouteRegistrationTarget.cs",
			"""internal static bool ShouldRegisterNewRoute() => false;""",
			"""internal static bool ShouldRegisterNewRoute() => true;""",
			ct);

		// After HR, insert the route (simulating what a hot-reload-aware
		// route registration system would do when it re-evaluates registrations).
		HotReloadRouteRegistrationTarget.ShouldRegisterNewRoute().Should().BeTrue(
			"C# HR should have flipped the flag to true");

		var resolver = app.Host.Services.GetRequiredService<IRouteResolver>();
		resolver.InsertRoute(new RouteInfo("DynamicNewRoute", View: () => typeof(HotReloadNewRoutePage)));

		// Navigate to the newly registered route.
		await app.FrameNavigator.NavigateRouteAsync(this, "DynamicNewRoute");
		await WaitForRouteAsync(app.NavigationRoot, app.FrameNavigator, "DynamicNewRoute", TimeSpan.FromSeconds(30), ct);

		var page = ResolveCurrentPage<HotReloadNewRoutePage>(app.NavigationRoot);
		page.Should().NotBeNull(
			"Navigation to dynamically-registered route should land on the page (#3072)");
		page!.DisplayedValue.Should().Be("new-route-loaded");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 19. Region.Name add via XAML HR (#3075)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR adds a Region.Name to a previously unnamed Grid child.
	/// After page replacement, navigation to the new region should succeed.
	/// This is a "working scenario — needs regression test" per #3075.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RegionNameAddedViaXamlHR_Then_NavigationResolvesNewRegion(CancellationToken ct)
	{
		await using var app = await SetupRegionNameAddRemoveAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadRegionNameAddRemovePage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Baseline: "ExistingRegion" is navigable but "AddedRegion" is not.
		var panelNav = await WaitForPanelNavigatorAsync(hostPage!.ContentGrid, TimeSpan.FromSeconds(30), ct);
		panelNav.Route?.Base.Should().Be("ExistingRegion",
			"Initial default region should be ExistingRegion");

		// XAML HR: add Region.Name="AddedRegion" to the unnamed child.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadRegionNameAddRemovePage.xaml",
			"""<Grid x:Name="_unnamedChild" />""",
			"""<Grid x:Name="_unnamedChild" uen:Region.Name="AddedRegion" />""",
			ct);

		// Wait for page replacement.
		var updatedPage = await WaitForPageReplacementAsync<HotReloadRegionNameAddRemovePage>(
			app.NavigationRoot, hostPage, TimeSpan.FromSeconds(30), ct);

		// Navigate to the newly-added region.
		var updatedPanelNav = await WaitForPanelNavigatorAsync(updatedPage.ContentGrid, TimeSpan.FromSeconds(30), ct);
		await updatedPanelNav.NavigateRouteAsync(this, "AddedRegion");
		await Task.Delay(500, ct);

		updatedPanelNav.Route?.Base.Should().Be("AddedRegion",
			"After XAML HR adds Region.Name, navigation should resolve the new region (#3075)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 20. Panel Region.Names swap via XAML HR (#2904)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Renaming Region.Name on a Panel child under a Visibility navigator
	/// via XAML HR. Bug #2904 reports blank content when Region.Names are renamed.
	/// This test verifies the Panel navigates correctly after the rename.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_PanelRegionNamesSwappedViaXamlHR_Then_NavigationStillWorks(CancellationToken ct)
	{
		await using var app = await SetupPanelRegionNamesAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadPanelRegionNamesPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Baseline: navigate to RegionTwo.
		var panelNav = await WaitForPanelNavigatorAsync(hostPage!.ContentGrid, TimeSpan.FromSeconds(30), ct);
		await panelNav.NavigateRouteAsync(this, "RegionTwo");
		await Task.Delay(500, ct);
		panelNav.Route?.Base.Should().Be("RegionTwo",
			"Pre-HR: should navigate to RegionTwo successfully");

		// XAML HR: rename "RegionOne" to "RegionRenamed" on the first child.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadPanelRegionNamesPage.xaml",
			"""uen:Region.Name="RegionOne" """,
			"""uen:Region.Name="RegionRenamed" """,
			ct);

		// Wait for page replacement after XAML HR.
		var renamedPage = await WaitForPageReplacementAsync<HotReloadPanelRegionNamesPage>(
			app.NavigationRoot, hostPage, TimeSpan.FromSeconds(30), ct);

		// After rename, navigate to "RegionRenamed" should work (not blank).
		var renamedPanelNav = await WaitForPanelNavigatorAsync(renamedPage.ContentGrid, TimeSpan.FromSeconds(30), ct);
		await renamedPanelNav.NavigateRouteAsync(this, "RegionRenamed");
		await Task.Delay(500, ct);

		renamedPanelNav.Route?.Base.Should().Be("RegionRenamed",
			"After XAML HR renames Region.Name on a Panel child, Visibility navigator should " +
			"resolve the renamed region without going blank (#2904)");

		// Also verify the other untouched region still works.
		await renamedPanelNav.NavigateRouteAsync(this, "RegionTwo");
		await Task.Delay(500, ct);

		renamedPanelNav.Route?.Base.Should().Be("RegionTwo",
			"Untouched region should remain navigable after rename (#2904)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 12. ViewModel (DataContext) survives XAML HR view swap
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Proves that when XAML HR replaces a page instance via <c>ReplaceViewInstance</c>,
	/// the new page's DataContext is re-injected by the navigation framework.
	/// Without the fix, the new <c>NavigationRegion</c> created by <c>InitializeComponent()</c>
	/// has <c>_wasUnloaded = false</c>, so <c>HandleLoaded</c> never re-cascades the parent
	/// route, leaving DataContext null on the new page.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_XamlHRReplacesPage_Then_ViewModelReinjected(CancellationToken ct)
	{
		await using var app = await SetupVmXamlPageAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadVmXamlPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Baseline: ViewModel should be injected as DataContext.
		hostPage!.DataContext.Should().BeOfType<HotReloadVm>(
			"ViewMap<HotReloadVmXamlPage, HotReloadVm> should bind the VM as DataContext");
		hostPage.Label.Text.Should().Be("before-hr");

		// XAML HR: change the TextBlock text to trigger a page swap.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadVmXamlPage.xaml",
			"""Text="before-hr" """,
			"""Text="after-hr" """,
			ct);

		// Wait for XAML HR to replace the page instance.
		var newPage = await WaitForPageReplacementAsync<HotReloadVmXamlPage>(
			app.NavigationRoot, hostPage, TimeSpan.FromSeconds(30), ct);

		// The new page should have the updated XAML content.
		newPage.Label.Text.Should().Be("after-hr",
			"XAML HR should have swapped in a page with the updated TextBlock text");

		// Critical assertion: ViewModel must be re-injected on the new page.
		newPage.DataContext.Should().BeOfType<HotReloadVm>(
			"After XAML HR page swap, the navigation framework should re-inject the ViewModel as DataContext");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 13. Pending failed-navigation retry after hot-reload
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Regression test for the Studio Live scaffold-then-hot-reload scenario.
	/// When an initial navigation fires before the target view type can be
	/// constructed (e.g. the user prompts an AI agent to build a new page, the
	/// route is registered but the type is not yet present in the assembly),
	/// <c>FrameNavigator.Show</c> catches the construction exception and
	/// returns null. Without recovery, the host stays on a blank/no-content
	/// state forever — every subsequent HR delta patches types that are never
	/// mounted in the visual tree.
	///
	/// This test reproduces the failure with a gate-controlled page whose
	/// constructor throws while <see cref="HotReloadPendingRetryGate"/> is
	/// closed, then verifies that <see cref="UI.NavigationRouteUpdateHandler"/>
	/// re-issues the recorded pending request once HR flips the gate and the
	/// resolver is rebuilt — without any explicit re-navigation by user code.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_NavTargetTypeNotYetConstructable_Then_HRTriggersPendingRetry(CancellationToken ct)
	{
		await using var app = await SetupPendingRetryAppAsync(ct);

		// Baseline: HotReloadPageOne is the working initial route.
		var firstPage = ResolveCurrentPage<HotReloadPageOne>(app.NavigationRoot);
		firstPage.Should().NotBeNull("Initial route should land on HotReloadPageOne");

		// Attempt to navigate to the gated page. Its constructor throws while
		// the gate is closed, so FrameNavigator.Show catches the exception and
		// returns null — ControlNavigator records the request as pending.
		// We do not await the result: the navigation completes (with
		// Route.Empty) but the visual tree is not updated to the target page.
		_ = app.FrameNavigator.NavigateRouteAsync(this, "HotReloadPendingRetryPage");

		// Give the failed navigation time to settle and the pending slot to be set.
		await Task.Delay(500, ct);

		ResolveCurrentPage<HotReloadPendingRetryPage>(app.NavigationRoot)
			.Should().BeNull(
				"With the gate closed the target page's constructor throws and the " +
				"navigation must NOT have landed on the target page yet.");

		// C# HR: flip the gate so the page constructor no longer throws.
		// NavigationRouteUpdateHandler.UpdateApplication rebuilds the route
		// resolver and the retry walk re-issues the pending failed request.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadPendingRetryGate.cs",
			"return false;",
			"return true;",
			ct);

		await WaitForRouteAsync(
			app.NavigationRoot,
			app.FrameNavigator,
			"HotReloadPendingRetryPage",
			TimeSpan.FromSeconds(30),
			ct);

		var pendingPage = ResolveCurrentPage<HotReloadPendingRetryPage>(app.NavigationRoot);
		pendingPage.Should().NotBeNull(
			"After HR flips the gate, NavigationRouteUpdateHandler must re-issue " +
			"the pending failed navigation request and the target page must mount " +
			"in the visual tree without any explicit re-navigation by user code.");
		pendingPage!.DisplayedValue.Should().Be("pending-retry-loaded",
			"The retried page instance must have been constructed by the post-HR retry.");
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
					.CreateDefaultBuilder(typeof(Given_NavigationHotReload).Assembly)
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

	/// <summary>XAML page with ViewModel binding for view-swap DataContext test.</summary>
	private static Task<HotReloadNavTestApp> SetupVmXamlPageAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadVmXamlPage, HotReloadVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadVmXamlPage", View: views.FindByView<HotReloadVmXamlPage>(), IsDefault: true),
					}));
			},
			"HotReloadVmXamlPage",
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

	/// <summary>Navigation.Data binding page → PageOne / PageTwo for #3077 test.</summary>
	private static Task<HotReloadNavTestApp> SetupNavDataBindingAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadNavDataBindingPage>(),
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadPageTwo>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadNavDataBindingPage", View: views.FindByView<HotReloadNavDataBindingPage>(), IsDefault: true),
						new RouteMap("PageOne", View: views.FindByView<HotReloadPageOne>()),
						new RouteMap("PageTwo", View: views.FindByView<HotReloadPageTwo>()),
					}));
			},
			"HotReloadNavDataBindingPage",
			ct);

	/// <summary>Frame.Navigate from event handler → PageOne / PageTwo for #3078 test.</summary>
	private static Task<HotReloadNavTestApp> SetupFrameNavAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadFrameNavPage>(),
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadPageTwo>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadFrameNavPage", View: views.FindByView<HotReloadFrameNavPage>(), IsDefault: true),
						new RouteMap("PageOne", View: views.FindByView<HotReloadPageOne>()),
						new RouteMap("PageTwo", View: views.FindByView<HotReloadPageTwo>()),
					}));
			},
			"HotReloadFrameNavPage",
			ct);

	/// <summary>VM constructor body test → PageOne / VmCtorPage for #3083 test.</summary>
	private static Task<HotReloadNavTestApp> SetupVmCtorAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadVmCtorPage, HotReloadVmCtorVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("VmCtorPage", View: views.FindByView<HotReloadVmCtorPage>()),
					}));
			},
			"HotReloadPageOne",
			ct);

	/// <summary>Region rename TabBar page for #3088 test.</summary>
	private static Task<HotReloadNavTestApp> SetupRegionRenameAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadRegionRenamePage>(),
					new ViewMap<HotReloadRegionContentPage, HotReloadRegionVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadRegionRenamePage",
							View: views.FindByView<HotReloadRegionRenamePage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("AlphaRegion", View: views.FindByView<HotReloadRegionContentPage>(), IsDefault: true),
								new RouteMap("BetaRegion", View: views.FindByView<HotReloadRegionContentPage>()),
								new RouteMap("GammaRegion", View: views.FindByView<HotReloadRegionContentPage>()),
							}),
					}));
			},
			"HotReloadRegionRenamePage",
			ct);

	/// <summary>IRouteNotifier test → PageOne / PageTwo for #3089 test.</summary>
	private static Task<HotReloadNavTestApp> SetupRouteNotifierAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadPageTwo>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("PageOne", View: views.FindByView<HotReloadPageOne>()),
						new RouteMap("PageTwo", View: views.FindByView<HotReloadPageTwo>()),
					}));
			},
			"HotReloadPageOne",
			ct);

	/// <summary>Flyout/dialog test → MainPage + FlyoutPage for #3079 test.</summary>
	private static Task<HotReloadNavTestApp> SetupFlyoutAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadFlyoutPage, HotReloadFlyoutVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("FlyoutPage", View: views.FindByView<HotReloadFlyoutPage>()),
					}));
			},
			"HotReloadPageOne",
			ct);

	/// <summary>Nav-data new-property test → PageOne + DataPage for #3080 test.</summary>
	private static Task<HotReloadNavTestApp> SetupNavDataNewPropAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new DataViewMap<HotReloadNavDataNewPropPage, HotReloadNavDataNewPropVm, HotReloadNavDataWithNewProp>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("NavDataNewPropPage", View: views.FindByView<HotReloadNavDataNewPropPage>()),
					}));
			},
			"HotReloadPageOne",
			ct);

	/// <summary>Optional VM ctor param test → PageOne + OptionalParamPage for #3081 test.</summary>
	private static Task<HotReloadNavTestApp> SetupOptionalParamAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadOptionalParamPage, HotReloadOptionalParamVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("OptionalParamPage", View: views.FindByView<HotReloadOptionalParamPage>()),
					}));
			},
			"HotReloadPageOne",
			ct);

	/// <summary>Param order test → PageOne + ParamOrderPage for #3082 test.</summary>
	private static Task<HotReloadNavTestApp> SetupParamOrderAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadParamOrderPage, HotReloadParamOrderVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("ParamOrderPage", View: views.FindByView<HotReloadParamOrderPage>()),
					}));
			},
			"HotReloadPageOne",
			ct);

	/// <summary>Pending failed-nav retry test → PageOne as initial, PendingRetryPage as gated target.</summary>
	private static Task<HotReloadNavTestApp> SetupPendingRetryAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>(),
					new ViewMap<HotReloadPendingRetryPage>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
						new RouteMap("HotReloadPendingRetryPage", View: views.FindByView<HotReloadPendingRetryPage>()),
					}));
			},
			"HotReloadPageOne",
			ct);

	/// <summary>Route registration test → PageOne only initially, for #3072 test.</summary>
	private static Task<HotReloadNavTestApp> SetupRouteRegistrationAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPageOne>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap("HotReloadPageOne", View: views.FindByView<HotReloadPageOne>(), IsDefault: true),
					}));
			},
			"HotReloadPageOne",
			ct);

	/// <summary>Region.Name add/remove test → ExistingRegion + unnamed child for #3075 test.</summary>
	private static Task<HotReloadNavTestApp> SetupRegionNameAddRemoveAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadRegionNameAddRemovePage>(),
					new ViewMap<HotReloadRegionContentPage, HotReloadRegionVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadRegionNameAddRemovePage",
							View: views.FindByView<HotReloadRegionNameAddRemovePage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("ExistingRegion", View: views.FindByView<HotReloadRegionContentPage>(), IsDefault: true),
								new RouteMap("AddedRegion", View: views.FindByView<HotReloadRegionContentPage>()),
							}),
					}));
			},
			"HotReloadRegionNameAddRemovePage",
			ct);

	/// <summary>Panel Region.Names swap test → RegionOne + RegionTwo + RegionRenamed for #2904 test.</summary>
	private static Task<HotReloadNavTestApp> SetupPanelRegionNamesAppAsync(CancellationToken ct)
		=> SetupAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadPanelRegionNamesPage>(),
					new ViewMap<HotReloadRegionContentPage, HotReloadRegionVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadPanelRegionNamesPage",
							View: views.FindByView<HotReloadPanelRegionNamesPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("RegionOne", View: views.FindByView<HotReloadRegionContentPage>(), IsDefault: true),
								new RouteMap("RegionTwo", View: views.FindByView<HotReloadRegionContentPage>()),
								new RouteMap("RegionRenamed", View: views.FindByView<HotReloadRegionContentPage>()),
							}),
					}));
			},
			"HotReloadPanelRegionNamesPage",
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
		public IHost Host => _host;

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
