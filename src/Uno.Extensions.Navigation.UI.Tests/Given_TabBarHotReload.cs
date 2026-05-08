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
using Uno.Toolkit.UI;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

[TestClass]
[RunsInSecondaryApp(ignoreIfNotSupported: true)]
public class Given_TabBarHotReload
{
	[TestInitialize]
	public void Setup()
	{
		HotReloadHelper.DefaultWorkspaceTimeout = TimeSpan.FromSeconds(300);
		HotReloadHelper.DefaultMetadataUpdateTimeout = TimeSpan.FromSeconds(60);
	}

	// ──────────────────────────────────────────────────────────────────────
	// 1. Basic tab switch after HR (2-tab)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// HR flips the target method, then switch to TabTwo → sees "updated".
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_SwitchTabAfterUpdate_Then_SelectedTabReflectsUpdate(CancellationToken ct)
	{
		await using var app = await SetupTwoTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadTabBarPage");

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage!.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.DisplayedValue.Should().Be("original",
			"TabOne's VM should read the pre-HR method body");

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTabBarTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			hostPage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabTwo");

		var tabTwoVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tabTwoVm.DisplayedValue.Should().Be("updated",
			"TabTwo's VM should read the post-HR method body");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 2. HR while on TabTwo, switch back to TabOne (2-tab)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Switch to TabTwo, apply HR, switch back to TabOne → TabOne's reused VM returns "updated".
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_UpdateWhileOnTabTwo_Then_SwitchBackToTabOneReflectsUpdate(CancellationToken ct)
	{
		await using var app = await SetupTwoTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadTabBarPage");

		var tabOneVmBefore = await WaitForTabContentVmAsync(
			hostPage!.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVmBefore.DisplayedValue.Should().Be("original");

		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			hostPage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabTwo");

		var tabTwoVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tabTwoVm.DisplayedValue.Should().Be("original",
			"TabTwo (pre-HR) should read 'original'");

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTabBarTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabOne");

		var tabOneVmAfter = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVmAfter.DisplayedValue.Should().Be("updated",
			"TabOne's VM should read the post-HR method body after switching back");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 3. Current tab reflects HR without switching (2-tab)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Verifies the currently-viewed tab's VM property reflects the HR'd method body
	/// without requiring a tab switch. Because <see cref="HotReloadTabBarVm.DisplayedValue"/>
	/// re-reads the target on every access, the change is visible immediately. Also verifies
	/// the TabBar's selected index is not disrupted by the HR delta.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_HRApplied_Then_CurrentTabReflectsUpdateAndSelectionPreserved(CancellationToken ct)
	{
		await using var app = await SetupTwoTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Switch to TabTwo so we're not on the default tab.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			hostPage!.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabTwo");

		var tabTwoVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tabTwoVm.DisplayedValue.Should().Be("original");
		hostPage.TabBar.SelectedIndex.Should().Be(1, "TabTwo is at index 1");

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTabBarTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Without switching tabs — re-read the VM property.
		tabTwoVm.DisplayedValue.Should().Be("updated",
			"Current tab's VM should reflect the HR'd method body without switching");
		hostPage.TabBar.SelectedIndex.Should().Be(1,
			"TabBar selected index should not change after HR");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 4. Unvisited third tab shows updated content (3-tab)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// A tab that was never visited before HR should show the updated value on first visit.
	/// This proves the navigation framework doesn't cache stale content for unvisited routes.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_UnvisitedTabVisitedAfterHR_Then_ShowsUpdatedContent(CancellationToken ct)
	{
		await using var app = await SetupThreeTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarThreeTabPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		// Only visit TabOne (IsDefault). TabTwo and TabThree are never visited.
		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage!.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.DisplayedValue.Should().Be("original");

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTabBarTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Switch directly to TabThree (never visited before).
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			hostPage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabThree");

		var tabThreeVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabThree", TimeSpan.FromSeconds(30), ct);
		tabThreeVm.DisplayedValue.Should().Be("updated",
			"An unvisited tab's fresh VM should read the post-HR method body");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 5. Rapid multi-tab switching after HR (3-tab)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// After HR, cycle through all three tabs and back. Every tab's VM should return
	/// "updated" and no tab should show blank/null content. Exercises the
	/// <c>PanelVisiblityNavigator</c>'s show/hide toggling under HR conditions.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CycleThroughAllTabsAfterHR_Then_AllReflectUpdate(CancellationToken ct)
	{
		await using var app = await SetupThreeTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarThreeTabPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage!.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.DisplayedValue.Should().Be("original");

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTabBarTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			hostPage.TabBar, TimeSpan.FromSeconds(30), ct);

		// Tab1 → Tab2
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabTwo");
		var tab2Vm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tab2Vm.DisplayedValue.Should().Be("updated", "TabTwo should reflect HR");

		// Tab2 → Tab3
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabThree");
		var tab3Vm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabThree", TimeSpan.FromSeconds(30), ct);
		tab3Vm.DisplayedValue.Should().Be("updated", "TabThree should reflect HR");

		// Tab3 → Tab1 (returning to previously-visited tab)
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabOne");
		var tab1VmAfter = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tab1VmAfter.DisplayedValue.Should().Be("updated", "TabOne should still reflect HR after full cycle");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 6. Gated tab route becomes navigable after HR (3-tab)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// A third tab's <c>RouteMap.Init</c> delegate gates navigation via
	/// <see cref="HotReloadTabGateTarget.IsAvailable"/>. While closed, navigating to TabThree
	/// redirects to TabOne. After HR flips the gate, TabThree resolves to its content page.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_GatedTabUnlockedByHR_Then_TabContentLoads(CancellationToken ct)
	{
		await using var app = await SetupGatedTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarThreeTabPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage!.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull("TabOne (IsDefault) should be loaded");

		// Pre-HR: gate closed — navigating to TabThree should redirect to TabOne.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			hostPage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabThree");

		// Poll until TabThree is confirmed absent (redirect to TabOne should happen quickly).
		await WaitForTabAbsentAsync(hostPage.ContentGrid, "TabThree", TimeSpan.FromSeconds(5), ct);
		var tabThreeBeforeHR = FindTabContentVm(hostPage.ContentGrid, "TabThree");
		tabThreeBeforeHR.Should().BeNull(
			"while the Init gate is closed, TabThree should not populate content");

		// HR: open the gate. Disposal reverts on scope exit.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTabGateTarget.cs",
			"return false;",
			"return true;",
			ct);

		// Post-HR: gate open — TabThree should now resolve.
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabThree");
		var tabThreeVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabThree", TimeSpan.FromSeconds(30), ct);
		tabThreeVm.Should().NotBeNull(
			"with the gate open post-HR, TabThree should load content");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 7. Region.Attached removed from TabBar via XAML HR (#2971)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR removes <c>uen:Region.Attached="True"</c> from the TabBar element.
	/// Without Region.Attached the TabBar no longer drives navigation.
	/// Bug #2971: after such a change the content area goes blank.
	/// XAML HR replaces the page instance — must re-resolve to check the NEW page.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RegionAttachedRemovedFromTabBarViaXamlHR_Then_ContentNotBlanked(CancellationToken ct)
	{
		await using var app = await SetupXamlTwoTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadTabBarXamlPage");

		// Baseline: TabOne (default) loaded.
		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage!.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull("TabOne content should be loaded before HR");

		// XAML HR: remove Region.Attached from the TabBar.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarXamlPage.xaml",
			"""<utu:TabBar x:Name="TB" Grid.Row="1" uen:Region.Attached="True">""",
			"""<utu:TabBar x:Name="TB" Grid.Row="1">""",
			ct);

		// Wait for XAML HR to replace the page instance.
		var activePage = await WaitForPageReplacementAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot, hostPage, TimeSpan.FromSeconds(10), ct);

		// The NEW page's content area should NOT be blank (#2971 causes this).
		activePage.ContentGrid.Children.Count.Should().BeGreaterThan(0,
			"Content area should not be blank after Region.Attached removal (#2971)");

		var tabOneVmAfter = FindTabContentVm(activePage.ContentGrid, "TabOne");
		tabOneVmAfter.Should().NotBeNull(
			"TabOne content should still be accessible after Region.Attached removal (#2971)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 8. Region.Name changed on TabBarItem via XAML HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR renames <c>Region.Name="TabTwo"</c> to <c>"TabTwoRenamed"</c>
	/// on the second TabBarItem. Route "TabTwoRenamed" is pre-registered so the
	/// SelectorNavigator can resolve it.
	/// XAML HR replaces the page instance — references must be re-resolved after HR.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RegionNameChangedOnTabBarItemViaXamlHR_Then_NavigationResolvesNewName(CancellationToken ct)
	{
		await using var app = await SetupXamlRenamedRouteAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage!.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull();

		// Pre-HR: navigate to TabTwo (original name).
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			hostPage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabTwo");

		var tabTwoVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tabTwoVm.Should().NotBeNull("TabTwo should be navigable before HR");

		// XAML HR: rename Region.Name on the second TabBarItem.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarXamlPage.xaml",
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""",
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwoRenamed" IsSelectable="True" />""",
			ct);

		// Wait for XAML HR to produce a page with the renamed region.
		// Uses content-aware polling because async XAML HR from a prior test's
		// file revert (same .xaml file) could race with this test's modification.
		var activePage = await WaitForPageMatchingAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot,
			page => page.TabBar.Items.OfType<FrameworkElement>()
				.Any(i => Uno.Extensions.Navigation.UI.Region.GetName(i) == "TabTwoRenamed"),
			TimeSpan.FromSeconds(30), ct);

		// Sanity-check: full region name list on the replaced page.
		var regionNames = activePage.TabBar.Items.OfType<FrameworkElement>()
			.Select(i => Uno.Extensions.Navigation.UI.Region.GetName(i))
			.ToList();
		regionNames.Should().Contain("TabTwoRenamed",
			"XAML HR should produce TabBarItems with the renamed Region.Name");

		// Navigate to the renamed route using the new page's navigator.
		var activeNavigator = await WaitForTabBarNavigatorAsync(
			activePage.TabBar, TimeSpan.FromSeconds(30), ct);
		await activeNavigator.NavigateRouteAsync(activePage, "TabTwoRenamed");

		var renamedVm = await WaitForTabContentVmAsync(
			activePage.ContentGrid, "TabTwoRenamed", TimeSpan.FromSeconds(30), ct);
		renamedVm.Should().NotBeNull(
			"Navigation should resolve the renamed Region.Name after XAML HR");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 9. TabBarItem added via XAML HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR adds a third <c>TabBarItem</c> with Region.Name="TabThree".
	/// The route is pre-registered in <see cref="SetupXamlThreeRouteAppAsync"/> so
	/// the SelectorNavigator can resolve it. In a real application, adding a new
	/// TabBarItem via XAML HR would also require updating the route registration
	/// in C# (which would itself be a separate HR delta). This test isolates the
	/// XAML-side behavior by pre-registering the route ahead of time.
	/// XAML HR replaces the page instance — references must be re-resolved after HR.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TabBarItemAddedViaXamlHR_Then_NewTabNavigable(CancellationToken ct)
	{
		await using var app = await SetupXamlThreeRouteAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		hostPage!.TabBar.Items.Count.Should().Be(2, "page starts with 2 tabs");

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull();

		// Build replacement with correct line endings (CRLF on Windows).
		var originalLine =
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""";
		var replacementLines =
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""" +
			Environment.NewLine + "\t\t\t" +
			"""<utu:TabBarItem Content="Tab Three" uen:Region.Name="TabThree" IsSelectable="True" />""";

		// XAML HR: add a third TabBarItem.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarXamlPage.xaml",
			originalLine,
			replacementLines,
			ct);

		// Wait for XAML HR to produce a page with 3 tabs.
		// Uses content-aware polling because async XAML HR from a prior test's
		// file revert (same .xaml file) could race with this test's modification.
		var activePage = await WaitForPageMatchingAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot,
			page => page.TabBar.Items.Count == 3,
			TimeSpan.FromSeconds(30), ct);

		// Sanity-check: the replaced page has 3 TabBarItems.
		activePage.TabBar.Items.Count.Should().Be(3,
			"XAML HR should have added a third TabBarItem on the replaced page");

		// Navigate to the new tab using the new page's navigator.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			activePage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(activePage, "TabThree");

		var tabThreeVm = await WaitForTabContentVmAsync(
			activePage.ContentGrid, "TabThree", TimeSpan.FromSeconds(30), ct);
		tabThreeVm.Should().NotBeNull(
			"Newly added TabThree should be navigable after XAML HR");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 10. Command binding removed + restored via XAML HR (#2912)
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Bug #2912: Re-adding a Button.Command binding via XAML HR breaks TabBar switching.
	/// Phase 1 removes the Command binding — tab switching should still work.
	/// Phase 2 restores it (via file revert) — #2912 reports tab switching breaks.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CommandBindingRemovedAndRestoredViaXamlHR_Then_TabSwitchingStillWorks(CancellationToken ct)
	{
		await using var app = await SetupCommandTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarCommandPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadTabBarCommandPage");

		// Baseline: tab switching works.
		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage!.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull();

		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			hostPage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabTwo");

		var tabTwoVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tabTwoVm.Should().NotBeNull("TabTwo should be navigable before HR");

		// Return to TabOne for the HR test.
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabOne");
		await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);

		// Phase 1: remove the Command binding.
		var revert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarCommandPage.xaml",
			"""Content="Navigate" Command="{Binding TestCommand}" """,
			"""Content="Navigate" """,
			ct);

		// Tab switching should still work without the Command.
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabTwo");
		var tabTwoAfterRemoval = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tabTwoAfterRemoval.Should().NotBeNull(
			"Tab switching should work after Command binding removal");

		// Phase 2: file revert re-adds the Command binding via XAML HR.
		await revert.DisposeAsync();

		// Wait for the reverted page to load (XAML HR replaces the page).
		await WaitForPageReplacementAsync<HotReloadTabBarCommandPage>(
			app.NavigationRoot, hostPage, TimeSpan.FromSeconds(10), ct);

		// #2912: tab switching should still work after Command is restored.
		// Re-resolve page and navigator since XAML HR replaced the instance.
		var revertedPage = ResolveCurrentPage<HotReloadTabBarCommandPage>(app.NavigationRoot)!;
		var revertedNavigator = await WaitForTabBarNavigatorAsync(
			revertedPage.TabBar, TimeSpan.FromSeconds(30), ct);
		await revertedNavigator.NavigateRouteAsync(revertedPage, "TabOne");
		var tabOneAfterRestore = await WaitForTabContentVmAsync(
			revertedPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneAfterRestore.Should().NotBeNull(
			"Tab switching should work after Command binding is restored (#2912)");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 11. Route added via C# HR for existing TabBarItem
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Real-world scenario: developer has a <c>TabBarItem</c> for "TabThree" in
	/// XAML but forgot to register the route in App.xaml.cs. The tab doesn't work.
	/// Developer then adds <c>new ("TabThree", View: views.FindByView&lt;…&gt;())</c>
	/// in code and saves — C# HR applies the delta.
	/// This test verifies whether the navigation framework picks up the new
	/// route after a C# hot-reload metadata update.
	/// Setup uses <see cref="HotReloadTabBarThreeTabPage"/> (3 tabs in code-behind)
	/// but only registers routes for TabOne and TabTwo. The route builder delegate
	/// conditionally includes TabThree based on
	/// <see cref="HotReloadRouteRegistration.IncludeTabThree"/>, which returns
	/// <c>false</c> initially. A C# HR then flips it to <c>true</c>, the
	/// framework re-invokes the delegate, and navigation to TabThree is attempted.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RouteAddedViaCSharpHR_Then_ExistingTabBarItemBecomesNavigable(CancellationToken ct)
	{
		// Boot with 3-tab page; route builder checks HotReloadRouteRegistration
		// which initially excludes TabThree.
		await using var app = await SetupThreeTabPartialRoutesAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarThreeTabPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();
		hostPage!.TabBar.Items.Count.Should().Be(3, "page has 3 TabBarItems in code-behind");

		// Baseline: TabOne loaded (default route).
		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull();

		// Pre-HR: TabThree has no route — navigation should not produce content.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			hostPage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabThree");
		var preHrVm = FindTabContentVm(hostPage.ContentGrid, "TabThree");
		// (Don't assert pass/fail here — just record pre-HR state.)

		// Return to TabOne before applying HR.
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabOne");
		await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);

		// C# HR: simulate developer adding the TabThree route.
		// Flipping HotReloadRouteRegistration.IncludeTabThree() from false → true
		// triggers a metadata update. The NavigationRouteUpdateHandler re-invokes
		// the route builder delegate which now includes TabThree.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadRouteRegistration.cs",
			"=> false",
			"=> true",
			ct);

		// Post-HR: try to navigate to TabThree again.
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabThree");

		var tabThreeVm = FindTabContentVm(hostPage.ContentGrid, "TabThree");
		tabThreeVm.Should().NotBeNull(
			"After C# HR adds the route, navigating to TabThree should work");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 12. Auto-resolve: XAML HR adds TabBarItem whose route name matches a
	//     type by convention (TabThree → TabThreePage), no explicit RouteMap
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Tests the <see cref="RouteResolverDefault"/> implicit mapping behaviour
	/// when a TabBarItem is added via XAML HR and its <c>Region.Name</c>
	/// matches a Page type by naming convention (route + "Page" suffix).
	///
	/// Setup: XAML 2-tab page with routes only for TabOne and TabTwo.
	/// There is NO explicit RouteMap for "TabThree", but the assembly
	/// contains <see cref="TabThreePage"/> which matches by convention.
	///
	/// XAML HR adds a third TabBarItem with <c>Region.Name="TabThree"</c>.
	/// The framework should auto-resolve "TabThree" → <c>TabThreePage</c>
	/// and navigate successfully.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TabBarItemAddedViaXamlHR_WithAutoResolvedRoute_Then_NavigationWorks(CancellationToken ct)
	{
		// Boot with 2-tab XAML page — NO RouteMap for "TabThree" but
		// TabThreePage exists in the assembly for auto-resolution.
		await using var app = await SetupXamlTwoTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();
		hostPage!.TabBar.Items.Count.Should().Be(2, "page starts with 2 tabs");

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull();

		// XAML HR: add a third TabBarItem with Region="TabThree".
		var originalLine =
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""";
		var replacementLines =
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""" +
			Environment.NewLine + "\t\t\t" +
			"""<utu:TabBarItem Content="Tab Three" uen:Region.Name="TabThree" IsSelectable="True" />""";

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarXamlPage.xaml",
			originalLine,
			replacementLines,
			ct);

		// Wait for XAML HR to produce a page with 3 tabs.
		var activePage = await WaitForPageMatchingAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot,
			page => page.TabBar.Items.Count == 3,
			TimeSpan.FromSeconds(30), ct);

		activePage.TabBar.Items.Count.Should().Be(3,
			"XAML HR should have added a third TabBarItem");

		// Navigate to TabThree — auto-resolve should find TabThreePage.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			activePage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(activePage, "TabThree");

		// The auto-resolve creates a FrameView → Frame → TabThreePage chain.
		// Poll until the TabThree region has content.
		var tabThreeContent = await WaitForTabRegionContentAsync(
			activePage.ContentGrid, "TabThree", typeof(TabThreePage),
			TimeSpan.FromSeconds(30), ct);
		tabThreeContent.Should().NotBeNull(
			"RouteResolverDefault should auto-resolve 'TabThree' → TabThreePage");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 13. XAML HR adds TabBarItem then C# HR adds RouteMap — both in
	//     sequence within the same test
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Simulates a developer who adds a new tab in XAML and then wires up
	/// the route in code. Both hot-reload steps happen in the same session.
	///
	/// Setup: XAML 2-tab page with routes only for TabOne and TabTwo.
	/// Step 1 — XAML HR adds <c>TabBarItem[Region="TabThree"]</c>.
	/// Step 2 — C# HR flips <see cref="HotReloadRouteRegistration.IncludeTabThree"/>
	///          from false → true, which adds the RouteMap for "TabThree".
	/// After both changes, navigation to "TabThree" should work.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_XamlHRAddsTabThenCSharpHRAddsRoute_Then_NavigationWorks(CancellationToken ct)
	{
		// Boot with 2-tab XAML page + partial route builder (TabThree gated).
		await using var app = await SetupXamlTwoTabPartialRoutesAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();
		hostPage!.TabBar.Items.Count.Should().Be(2, "page starts with 2 tabs");

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull();

		// Step 1 — XAML HR: add TabBarItem for TabThree.
		var originalLine =
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""";
		var replacementLines =
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""" +
			Environment.NewLine + "\t\t\t" +
			"""<utu:TabBarItem Content="Tab Three" uen:Region.Name="TabThree" IsSelectable="True" />""";

		await using var xamlRevert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarXamlPage.xaml",
			originalLine,
			replacementLines,
			ct);

		var activePage = await WaitForPageMatchingAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot,
			page => page.TabBar.Items.Count == 3,
			TimeSpan.FromSeconds(30), ct);

		activePage.TabBar.Items.Count.Should().Be(3,
			"XAML HR should have added a third TabBarItem");

		// Step 2 — C# HR: add the RouteMap for TabThree.
		await using var csharpRevert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadRouteRegistration.cs",
			"=> false",
			"=> true",
			ct);

		// Navigate to TabThree.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			activePage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(activePage, "TabThree");

		var tabThreeVm = await WaitForTabContentVmAsync(
			activePage.ContentGrid, "TabThree", TimeSpan.FromSeconds(30), ct);
		tabThreeVm.Should().NotBeNull(
			"After XAML HR adds the tab and C# HR adds the route, navigation should work");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 14. C# HR adds RouteMap first, then XAML HR adds the TabBarItem
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Reverse order of Test 13: the route is added via C# HR first, then
	/// the TabBarItem is added via XAML HR.
	///
	/// Setup: XAML 2-tab page, route builder conditionally includes TabThree.
	/// Step 1 — C# HR flips IncludeTabThree → true (route exists, no tab).
	/// Step 2 — XAML HR adds <c>TabBarItem[Region="TabThree"]</c>.
	/// Navigation to "TabThree" should work.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_CSharpHRAddsRouteThenXamlHRAddsTab_Then_NavigationWorks(CancellationToken ct)
	{
		// Boot with 2-tab XAML page + partial route builder (TabThree gated).
		await using var app = await SetupXamlTwoTabPartialRoutesAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();
		hostPage!.TabBar.Items.Count.Should().Be(2, "page starts with 2 tabs");

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull();

		// Step 1 — C# HR: add the RouteMap for TabThree (tab doesn't exist yet).
		await using var csharpRevert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadRouteRegistration.cs",
			"=> false",
			"=> true",
			ct);

		// Step 2 — XAML HR: add TabBarItem for TabThree.
		var originalLine =
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""";
		var replacementLines =
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""" +
			Environment.NewLine + "\t\t\t" +
			"""<utu:TabBarItem Content="Tab Three" uen:Region.Name="TabThree" IsSelectable="True" />""";

		await using var xamlRevert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarXamlPage.xaml",
			originalLine,
			replacementLines,
			ct);

		var activePage = await WaitForPageMatchingAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot,
			page => page.TabBar.Items.Count == 3,
			TimeSpan.FromSeconds(30), ct);

		activePage.TabBar.Items.Count.Should().Be(3,
			"XAML HR should have added a third TabBarItem");

		// Navigate to TabThree.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			activePage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(activePage, "TabThree");

		var tabThreeVm = await WaitForTabContentVmAsync(
			activePage.ContentGrid, "TabThree", TimeSpan.FromSeconds(30), ct);
		tabThreeVm.Should().NotBeNull(
			"After C# HR adds the route and XAML HR adds the tab, navigation should work");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 15. TabBarItem removed via XAML HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR removes the second TabBarItem (TabTwo) from a 2-tab page.
	/// After HR, only TabOne should remain. Navigation should continue to
	/// work on the remaining tab and the removed tab should not be
	/// accessible.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TabBarItemRemovedViaXamlHR_Then_RemainingTabStillWorks(CancellationToken ct)
	{
		await using var app = await SetupXamlTwoTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();
		hostPage!.TabBar.Items.Count.Should().Be(2, "page starts with 2 tabs");

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull();

		// XAML HR: remove the TabTwo TabBarItem entirely.
		var originalLines =
			"""<utu:TabBarItem Content="Tab One" uen:Region.Name="TabOne" IsSelectable="True" />""" +
			Environment.NewLine + "\t\t\t" +
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""";
		var replacementLine =
			"""<utu:TabBarItem Content="Tab One" uen:Region.Name="TabOne" IsSelectable="True" />""";

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarXamlPage.xaml",
			originalLines,
			replacementLine,
			ct);

		// Wait for XAML HR to produce a page with only 1 tab.
		var activePage = await WaitForPageMatchingAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot,
			page => page.TabBar.Items.Count == 1,
			TimeSpan.FromSeconds(30), ct);

		activePage.TabBar.Items.Count.Should().Be(1,
			"XAML HR should have removed TabTwo, leaving only TabOne");

		// TabOne should still be navigable on the new page.
		var tabOneVmAfter = await WaitForTabContentVmAsync(
			activePage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVmAfter.Should().NotBeNull(
			"TabOne navigation should still work after TabTwo is removed via XAML HR");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 16. TabBarItem order swapped via XAML HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR swaps the order of TabBarItems (TabTwo first, TabOne second).
	/// Both tabs should remain navigable after the reorder.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TabBarItemOrderSwappedViaXamlHR_Then_BothTabsNavigable(CancellationToken ct)
	{
		await using var app = await SetupXamlTwoTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();
		hostPage!.TabBar.Items.Count.Should().Be(2);

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull();

		// XAML HR: swap the order — TabTwo first, TabOne second.
		var originalLines =
			"""<utu:TabBarItem Content="Tab One" uen:Region.Name="TabOne" IsSelectable="True" />""" +
			Environment.NewLine + "\t\t\t" +
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""";
		var swappedLines =
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""" +
			Environment.NewLine + "\t\t\t" +
			"""<utu:TabBarItem Content="Tab One" uen:Region.Name="TabOne" IsSelectable="True" />""";

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarXamlPage.xaml",
			originalLines,
			swappedLines,
			ct);

		// Wait for XAML HR to produce a page where first item is TabTwo.
		var activePage = await WaitForPageMatchingAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot,
			page =>
			{
				var firstItem = page.TabBar.Items.OfType<FrameworkElement>().FirstOrDefault();
				return firstItem is not null &&
					   Uno.Extensions.Navigation.UI.Region.GetName(firstItem) == "TabTwo";
			},
			TimeSpan.FromSeconds(30), ct);

		activePage.TabBar.Items.Count.Should().Be(2,
			"Tab count should still be 2 after reorder");

		// Navigate to TabTwo (now first).
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			activePage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(activePage, "TabTwo");

		var tabTwoVm = await WaitForTabContentVmAsync(
			activePage.ContentGrid, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tabTwoVm.Should().NotBeNull("TabTwo should be navigable after reorder");

		// Navigate to TabOne (now second).
		await tabBarNavigator.NavigateRouteAsync(activePage, "TabOne");

		var tabOneVmAfter = await WaitForTabContentVmAsync(
			activePage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVmAfter.Should().NotBeNull("TabOne should be navigable after reorder");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 17. TabBarItem added, removed, then re-added via XAML HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Exercises HR state management: add TabThree, revert (auto-dispose removes
	/// it), then add it again. Ensures the navigator/region state is not stale
	/// from the first add.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TabBarItemAddedRemovedReaddedViaXamlHR_Then_NavigationStillWorks(CancellationToken ct)
	{
		await using var app = await SetupXamlThreeRouteAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();
		hostPage!.TabBar.Items.Count.Should().Be(2, "page starts with 2 tabs");

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull();

		var originalLine =
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""";
		var withThirdTab =
			"""<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""" +
			Environment.NewLine + "\t\t\t" +
			"""<utu:TabBarItem Content="Tab Three" uen:Region.Name="TabThree" IsSelectable="True" />""";

		// Step 1 — Add TabThree.
		var addRevert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarXamlPage.xaml",
			originalLine,
			withThirdTab,
			ct);

		var pageWith3 = await WaitForPageMatchingAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot,
			page => page.TabBar.Items.Count == 3,
			TimeSpan.FromSeconds(30), ct);
		pageWith3.TabBar.Items.Count.Should().Be(3);

		// Navigate to TabThree to populate it.
		var nav3 = await WaitForTabBarNavigatorAsync(pageWith3.TabBar, TimeSpan.FromSeconds(30), ct);
		await nav3.NavigateRouteAsync(pageWith3, "TabThree");
		var tabThreeVm = await WaitForTabContentVmAsync(
			pageWith3.ContentGrid, "TabThree", TimeSpan.FromSeconds(30), ct);
		tabThreeVm.Should().NotBeNull("TabThree should be navigable after first add");

		// Step 2 — Revert (removes TabThree).
		await addRevert.DisposeAsync();

		var pageWith2 = await WaitForPageMatchingAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot,
			page => page.TabBar.Items.Count == 2,
			TimeSpan.FromSeconds(30), ct);
		pageWith2.TabBar.Items.Count.Should().Be(2, "TabThree should be gone after revert");

		// Step 3 — Re-add TabThree.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarXamlPage.xaml",
			originalLine,
			withThirdTab,
			ct);

		var pageWith3Again = await WaitForPageMatchingAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot,
			page => page.TabBar.Items.Count == 3,
			TimeSpan.FromSeconds(30), ct);
		pageWith3Again.TabBar.Items.Count.Should().Be(3, "TabThree should be back after re-add");

		// Navigate to TabThree again — state should not be stale.
		var navAgain = await WaitForTabBarNavigatorAsync(
			pageWith3Again.TabBar, TimeSpan.FromSeconds(30), ct);
		await navAgain.NavigateRouteAsync(pageWith3Again, "TabThree");

		var tabThreeVmAgain = await WaitForTabContentVmAsync(
			pageWith3Again.ContentGrid, "TabThree", TimeSpan.FromSeconds(30), ct);
		tabThreeVmAgain.Should().NotBeNull(
			"TabThree should be navigable after add → remove → re-add cycle");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 18. TabBarItem Content text changed via XAML HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Simplest XAML HR scenario: change the display text of a TabBarItem.
	/// Navigation should continue to work — the Region.Name is unchanged,
	/// only the visual label changes.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TabBarItemContentChangedViaXamlHR_Then_NavigationPreserved(CancellationToken ct)
	{
		await using var app = await SetupXamlTwoTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage!.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull();

		// XAML HR: change "Tab One" to "First Tab".
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarXamlPage.xaml",
			"""<utu:TabBarItem Content="Tab One" uen:Region.Name="TabOne" IsSelectable="True" />""",
			"""<utu:TabBarItem Content="First Tab" uen:Region.Name="TabOne" IsSelectable="True" />""",
			ct);

		// Wait for XAML HR to produce a page where the first tab has updated text.
		var activePage = await WaitForPageMatchingAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot,
			page =>
			{
				var firstItem = page.TabBar.Items.OfType<TabBarItem>().FirstOrDefault();
				return firstItem is not null && firstItem.Content as string == "First Tab";
			},
			TimeSpan.FromSeconds(30), ct);

		// Region.Name should be unchanged.
		var firstRegion = Uno.Extensions.Navigation.UI.Region.GetName(
			activePage.TabBar.Items.OfType<FrameworkElement>().First());
		firstRegion.Should().Be("TabOne", "Region.Name should be unchanged after content text HR");

		// Navigate to TabTwo and back to TabOne to verify navigation works.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			activePage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(activePage, "TabTwo");

		var tabTwoVm = await WaitForTabContentVmAsync(
			activePage.ContentGrid, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tabTwoVm.Should().NotBeNull("TabTwo should be navigable after content text change");

		await tabBarNavigator.NavigateRouteAsync(activePage, "TabOne");

		var tabOneVmAfter = await WaitForTabContentVmAsync(
			activePage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVmAfter.Should().NotBeNull("TabOne should be navigable after content text change");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 19. Region.Attached removed then re-added to TabBar via XAML HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Counterpart to Test 7: remove Region.Attached from the TabBar, then
	/// re-add it. After the re-add, TabBar-driven navigation should be
	/// restored. This exercises whether the region system can recover
	/// from a detach → re-attach cycle.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RegionAttachedRemovedThenReaddedViaXamlHR_Then_NavigationRestored(CancellationToken ct)
	{
		await using var app = await SetupXamlTwoTabAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull();

		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage!.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull();

		// Step 1 — Remove Region.Attached from TabBar.
		var removeRevert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarXamlPage.xaml",
			"""<utu:TabBar x:Name="TB" Grid.Row="1" uen:Region.Attached="True">""",
			"""<utu:TabBar x:Name="TB" Grid.Row="1">""",
			ct);

		var detachedPage = await WaitForPageReplacementAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot, hostPage, TimeSpan.FromSeconds(30), ct);

		// Step 2 — Re-add Region.Attached (revert the removal).
		await removeRevert.DisposeAsync();

		var restoredPage = await WaitForPageReplacementAsync<HotReloadTabBarXamlPage>(
			app.NavigationRoot, detachedPage, TimeSpan.FromSeconds(30), ct);

		// After re-attachment, navigate to TabTwo to verify navigation works.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			restoredPage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(restoredPage, "TabTwo");

		var tabTwoVm = await WaitForTabContentVmAsync(
			restoredPage.ContentGrid, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tabTwoVm.Should().NotBeNull(
			"Navigation should be restored after Region.Attached is re-added via XAML HR");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 20. Entire TabBar navigation engine added via XAML HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// The page starts as a plain TextBlock with no navigation engine. XAML HR
	/// replaces the entire content with a Grid containing a content area
	/// (Region.Navigator="Visibility") and a TabBar with two items.
	/// After HR, the framework should detect the new navigation engine and
	/// kick off the initial tab navigation automatically, populating the
	/// first tab's content without any manual click.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TabBarAddedViaXamlHR_Then_InitialTabNavigationKicksIn(CancellationToken ct)
	{
		await using var app = await SetupLateAddTabBarAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarLateAddPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadTabBarLateAddPage");

		// Baseline: page has no TabBar and no content grid yet.
		hostPage!.TabBar.Should().BeNull("page should start without a TabBar");
		hostPage.ContentGrid.Should().BeNull("page should start without a content grid");

		// XAML HR: replace the placeholder with a full TabBar layout.
		var originalContent = """<TextBlock x:Name="_placeholder" Text="No TabBar yet" />""";
		var replacementContent =
			"""<Grid.RowDefinitions>""" + Environment.NewLine +
			"\t\t\t" + """<RowDefinition />""" + Environment.NewLine +
			"\t\t\t" + """<RowDefinition Height="Auto" />""" + Environment.NewLine +
			"\t\t" + """</Grid.RowDefinitions>""" + Environment.NewLine +
			"\t\t" + """<Grid x:Name="_contentGrid" Grid.Row="0" uen:Region.Attached="True" uen:Region.Navigator="Visibility" />""" + Environment.NewLine +
			"\t\t" + """<utu:TabBar x:Name="TB" Grid.Row="1" uen:Region.Attached="True">""" + Environment.NewLine +
			"\t\t\t" + """<utu:TabBarItem Content="Tab One" uen:Region.Name="TabOne" IsSelectable="True" />""" + Environment.NewLine +
			"\t\t\t" + """<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""" + Environment.NewLine +
			"\t\t" + """</utu:TabBar>""";

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarLateAddPage.xaml",
			originalContent,
			replacementContent,
			ct);

		// Wait for XAML HR to produce a page with a TabBar.
		var activePage = await WaitForPageMatchingAsync<HotReloadTabBarLateAddPage>(
			app.NavigationRoot,
			page => page.TabBar is not null && page.TabBar.Items.Count == 2,
			TimeSpan.FromSeconds(30), ct);

		activePage.TabBar.Should().NotBeNull("XAML HR should have added a TabBar");
		activePage.ContentGrid.Should().NotBeNull("XAML HR should have added a content grid");
		activePage.TabBar!.Items.Count.Should().Be(2, "TabBar should have two items");

		// The initial tab navigation should have kicked in automatically.
		// TabOne is the default route, so its content should be populated.
		var tabOneVm = await WaitForTabContentVmAsync(
			activePage.ContentGrid!, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull(
			"After XAML HR adds a new TabBar, the initial tab navigation should " +
			"kick in automatically and populate TabOne's content");

		// Also verify that manual navigation to TabTwo works.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			activePage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(activePage, "TabTwo");

		var tabTwoVm = await WaitForTabContentVmAsync(
			activePage.ContentGrid!, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tabTwoVm.Should().NotBeNull(
			"Manual navigation to TabTwo should work after TabBar is added via XAML HR");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 21. XAML HR adds TabBar, then C# HR injects route registration
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// The page starts as a plain TextBlock. XAML HR adds a TabBar with
	/// TabOne, TabTwo, and TabThree items. However, only TabOne and TabTwo
	/// have pre-registered routes. C# HR then injects the TabThree route
	/// registration, making it navigable.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_TabBarAddedViaXamlHR_ThenCSharpHRInjectsRoute_Then_NewTabNavigable(CancellationToken ct)
	{
		await using var app = await SetupLateAddTabBarPartialRoutesAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarLateAddPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadTabBarLateAddPage");

		// Baseline: page has no TabBar yet.
		hostPage!.TabBar.Should().BeNull("page should start without a TabBar");

		// XAML HR: replace the placeholder with a full TabBar layout (3 tabs).
		var originalContent = """<TextBlock x:Name="_placeholder" Text="No TabBar yet" />""";
		var replacementContent =
			"""<Grid.RowDefinitions>""" + Environment.NewLine +
			"\t\t\t" + """<RowDefinition />""" + Environment.NewLine +
			"\t\t\t" + """<RowDefinition Height="Auto" />""" + Environment.NewLine +
			"\t\t" + """</Grid.RowDefinitions>""" + Environment.NewLine +
			"\t\t" + """<Grid x:Name="_contentGrid" Grid.Row="0" uen:Region.Attached="True" uen:Region.Navigator="Visibility" />""" + Environment.NewLine +
			"\t\t" + """<utu:TabBar x:Name="TB" Grid.Row="1" uen:Region.Attached="True">""" + Environment.NewLine +
			"\t\t\t" + """<utu:TabBarItem Content="Tab One" uen:Region.Name="TabOne" IsSelectable="True" />""" + Environment.NewLine +
			"\t\t\t" + """<utu:TabBarItem Content="Tab Two" uen:Region.Name="TabTwo" IsSelectable="True" />""" + Environment.NewLine +
			"\t\t\t" + """<utu:TabBarItem Content="Tab Three" uen:Region.Name="TabThree" IsSelectable="True" />""" + Environment.NewLine +
			"\t\t" + """</utu:TabBar>""";

		await using var xamlRevert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarLateAddPage.xaml",
			originalContent,
			replacementContent,
			ct);

		// Wait for XAML HR to produce a page with a TabBar.
		var activePage = await WaitForPageMatchingAsync<HotReloadTabBarLateAddPage>(
			app.NavigationRoot,
			page => page.TabBar is not null && page.TabBar.Items.Count == 3,
			TimeSpan.FromSeconds(30), ct);

		activePage.TabBar.Should().NotBeNull("XAML HR should have added a TabBar");
		activePage.ContentGrid.Should().NotBeNull("XAML HR should have added a content grid");
		activePage.TabBar!.Items.Count.Should().Be(3, "TabBar should have three items");

		// Navigate to TabOne (pre-registered route) to verify basic navigation works.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			activePage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(activePage, "TabOne");

		var tabOneVm = await WaitForTabContentVmAsync(
			activePage.ContentGrid!, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.Should().NotBeNull("TabOne should be navigable (pre-registered route)");

		// C# HR: inject the TabThree route registration.
		await using var csRevert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadRouteRegistration.cs",
			"=> false",
			"=> true",
			ct);

		// Post-HR: navigate to TabThree — route should now be available.
		await tabBarNavigator.NavigateRouteAsync(activePage, "TabThree");

		var tabThreeVm = await WaitForTabContentVmAsync(
			activePage.ContentGrid!, "TabThree", TimeSpan.FromSeconds(30), ct);
		tabThreeVm.Should().NotBeNull(
			"After XAML HR adds TabBar and C# HR injects the route, TabThree should be navigable");
	}

	#region Setup helpers

	/// <summary>
	/// Generic setup: boots an Uno host with Toolkit navigation, mounts the nav root,
	/// and waits for the initial route to land.
	/// </summary>
	private static async Task<TabBarTestApp> SetupTabBarAppAsync(
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
					.CreateDefaultBuilder(typeof(Given_TabBarHotReload).Assembly)
					.UseToolkitNavigation()
					.UseNavigation(viewRouteBuilder: registerViewsAndRoutes)
					.Build(),
				navigationRoot: navigationRoot,
				initialRoute: initialRoute);

			var frameNav = await WaitForFrameNavigatorAsync(navigationRoot, TimeSpan.FromSeconds(30), ct);
			await WaitForRouteAsync(navigationRoot, frameNav, initialRoute, TimeSpan.FromSeconds(30), ct);

			return new TabBarTestApp(navigationRoot, frameNav, host);
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

	/// <summary>2-tab TabBar (TabOne, TabTwo).</summary>
	private static Task<TabBarTestApp> SetupTwoTabAppAsync(CancellationToken ct)
		=> SetupTabBarAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadTabBarPage>(),
					new ViewMap<HotReloadTabContentPage, HotReloadTabBarVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadTabBarPage",
							View: views.FindByView<HotReloadTabBarPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("TabOne", View: views.FindByView<HotReloadTabContentPage>(), IsDefault: true),
								new RouteMap("TabTwo", View: views.FindByView<HotReloadTabContentPage>()),
							}),
					}));
			},
			"HotReloadTabBarPage",
			ct);

	/// <summary>3-tab TabBar (TabOne, TabTwo, TabThree).</summary>
	private static Task<TabBarTestApp> SetupThreeTabAppAsync(CancellationToken ct)
		=> SetupTabBarAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadTabBarThreeTabPage>(),
					new ViewMap<HotReloadTabContentPage, HotReloadTabBarVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadTabBarThreeTabPage",
							View: views.FindByView<HotReloadTabBarThreeTabPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("TabOne", View: views.FindByView<HotReloadTabContentPage>(), IsDefault: true),
								new RouteMap("TabTwo", View: views.FindByView<HotReloadTabContentPage>()),
								new RouteMap("TabThree", View: views.FindByView<HotReloadTabContentPage>()),
							}),
					}));
			},
			"HotReloadTabBarThreeTabPage",
			ct);

	/// <summary>
	/// 3-tab TabBar where TabThree has a <c>RouteMap.Init</c> gate driven by
	/// <see cref="HotReloadTabGateTarget.IsAvailable"/>. When the gate is closed the Init
	/// redirects to TabOne; when open the request passes through unchanged.
	/// </summary>
	private static Task<TabBarTestApp> SetupGatedTabAppAsync(CancellationToken ct)
		=> SetupTabBarAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadTabBarThreeTabPage>(),
					new ViewMap<HotReloadTabContentPage, HotReloadTabBarVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadTabBarThreeTabPage",
							View: views.FindByView<HotReloadTabBarThreeTabPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("TabOne", View: views.FindByView<HotReloadTabContentPage>(), IsDefault: true),
								new RouteMap("TabTwo", View: views.FindByView<HotReloadTabContentPage>()),
								new RouteMap(
									"TabThree",
									View: views.FindByView<HotReloadTabContentPage>(),
									Init: request =>
										HotReloadTabGateTarget.IsAvailable()
											? request
											: request with { Route = request.Route with { Base = "TabOne" } }),
							}),
					}));
			},
			"HotReloadTabBarThreeTabPage",
			ct);

	/// <summary>
	/// 3-tab code-behind page whose route builder conditionally includes
	/// TabThree based on <see cref="HotReloadRouteRegistration.IncludeTabThree"/>.
	/// Initially only TabOne and TabTwo are registered.
	/// </summary>
	private static Task<TabBarTestApp> SetupThreeTabPartialRoutesAppAsync(CancellationToken ct)
		=> SetupTabBarAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadTabBarThreeTabPage>(),
					new ViewMap<HotReloadTabContentPage, HotReloadTabBarVm>());

				var nested = new List<RouteMap>
				{
					new RouteMap("TabOne", View: views.FindByView<HotReloadTabContentPage>(), IsDefault: true),
					new RouteMap("TabTwo", View: views.FindByView<HotReloadTabContentPage>()),
				};

				// Conditionally add TabThree — initially false, flipped to true via C# HR.
				if (HotReloadRouteRegistration.IncludeTabThree())
				{
					nested.Add(new RouteMap("TabThree", View: views.FindByView<HotReloadTabContentPage>()));
				}

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadTabBarThreeTabPage",
							View: views.FindByView<HotReloadTabBarThreeTabPage>(),
							IsDefault: true,
							Nested: nested.ToArray()),
					}));
			},
			"HotReloadTabBarThreeTabPage",
			ct);

	/// <summary>XAML-defined 2-tab TabBar page (TabOne, TabTwo).</summary>
	private static Task<TabBarTestApp> SetupXamlTwoTabAppAsync(CancellationToken ct)
		=> SetupTabBarAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadTabBarXamlPage>(),
					new ViewMap<HotReloadTabContentPage, HotReloadTabBarVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadTabBarXamlPage",
							View: views.FindByView<HotReloadTabBarXamlPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("TabOne", View: views.FindByView<HotReloadTabContentPage>(), IsDefault: true),
								new RouteMap("TabTwo", View: views.FindByView<HotReloadTabContentPage>()),
							}),
					}));
			},
			"HotReloadTabBarXamlPage",
			ct);

	/// <summary>XAML 2-tab page with pre-registered "TabTwoRenamed" route for rename testing.</summary>
	private static Task<TabBarTestApp> SetupXamlRenamedRouteAppAsync(CancellationToken ct)
		=> SetupTabBarAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadTabBarXamlPage>(),
					new ViewMap<HotReloadTabContentPage, HotReloadTabBarVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadTabBarXamlPage",
							View: views.FindByView<HotReloadTabBarXamlPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("TabOne", View: views.FindByView<HotReloadTabContentPage>(), IsDefault: true),
								new RouteMap("TabTwo", View: views.FindByView<HotReloadTabContentPage>()),
								new RouteMap("TabTwoRenamed", View: views.FindByView<HotReloadTabContentPage>()),
							}),
					}));
			},
			"HotReloadTabBarXamlPage",
			ct);

	/// <summary>XAML 2-tab page with pre-registered "TabThree" route for add-item testing.</summary>
	private static Task<TabBarTestApp> SetupXamlThreeRouteAppAsync(CancellationToken ct)
		=> SetupTabBarAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadTabBarXamlPage>(),
					new ViewMap<HotReloadTabContentPage, HotReloadTabBarVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadTabBarXamlPage",
							View: views.FindByView<HotReloadTabBarXamlPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("TabOne", View: views.FindByView<HotReloadTabContentPage>(), IsDefault: true),
								new RouteMap("TabTwo", View: views.FindByView<HotReloadTabContentPage>()),
								new RouteMap("TabThree", View: views.FindByView<HotReloadTabContentPage>()),
							}),
					}));
			},
			"HotReloadTabBarXamlPage",
			ct);

	/// <summary>
	/// XAML 2-tab page whose route builder conditionally includes TabThree
	/// based on <see cref="HotReloadRouteRegistration.IncludeTabThree"/>.
	/// Initially only TabOne and TabTwo routes are registered.
	/// </summary>
	private static Task<TabBarTestApp> SetupXamlTwoTabPartialRoutesAppAsync(CancellationToken ct)
		=> SetupTabBarAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadTabBarXamlPage>(),
					new ViewMap<HotReloadTabContentPage, HotReloadTabBarVm>());

				var nested = new List<RouteMap>
				{
					new RouteMap("TabOne", View: views.FindByView<HotReloadTabContentPage>(), IsDefault: true),
					new RouteMap("TabTwo", View: views.FindByView<HotReloadTabContentPage>()),
				};

				// Conditionally add TabThree — initially false, flipped to true via C# HR.
				if (HotReloadRouteRegistration.IncludeTabThree())
				{
					nested.Add(new RouteMap("TabThree", View: views.FindByView<HotReloadTabContentPage>()));
				}

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadTabBarXamlPage",
							View: views.FindByView<HotReloadTabBarXamlPage>(),
							IsDefault: true,
							Nested: nested.ToArray()),
					}));
			},
			"HotReloadTabBarXamlPage",
			ct);

	/// <summary>Command page with TabBar (TabOne, TabTwo) + Button Command binding for #2912 testing.</summary>
	private static Task<TabBarTestApp> SetupCommandTabAppAsync(CancellationToken ct)
		=> SetupTabBarAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadTabBarCommandPage>(),
					new ViewMap<HotReloadTabContentPage, HotReloadTabBarVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadTabBarCommandPage",
							View: views.FindByView<HotReloadTabBarCommandPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("TabOne", View: views.FindByView<HotReloadTabContentPage>(), IsDefault: true),
								new RouteMap("TabTwo", View: views.FindByView<HotReloadTabContentPage>()),
							}),
					}));
			},
			"HotReloadTabBarCommandPage",
			ct);

	/// <summary>
	/// Late-add page: starts as a plain TextBlock, XAML HR adds the TabBar layout.
	/// Routes for TabOne/TabTwo are pre-registered so navigation works once the TabBar appears.
	/// </summary>
	private static Task<TabBarTestApp> SetupLateAddTabBarAppAsync(CancellationToken ct)
		=> SetupTabBarAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadTabBarLateAddPage>(),
					new ViewMap<HotReloadTabContentPage, HotReloadTabBarVm>());

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadTabBarLateAddPage",
							View: views.FindByView<HotReloadTabBarLateAddPage>(),
							IsDefault: true,
							Nested: new RouteMap[]
							{
								new RouteMap("TabOne", View: views.FindByView<HotReloadTabContentPage>(), IsDefault: true),
								new RouteMap("TabTwo", View: views.FindByView<HotReloadTabContentPage>()),
							}),
					}));
			},
			"HotReloadTabBarLateAddPage",
			ct);

	/// <summary>
	/// Late-add page with partial routes: starts as a plain TextBlock, XAML HR adds the
	/// TabBar layout. Only TabOne/TabTwo have routes initially; TabThree is conditionally
	/// added via <see cref="HotReloadRouteRegistration.IncludeTabThree"/>.
	/// </summary>
	private static Task<TabBarTestApp> SetupLateAddTabBarPartialRoutesAppAsync(CancellationToken ct)
		=> SetupTabBarAppAsync(
			(views, routes) =>
			{
				views.Register(
					new ViewMap<HotReloadTabBarLateAddPage>(),
					new ViewMap<HotReloadTabContentPage, HotReloadTabBarVm>());

				var nested = new List<RouteMap>
				{
					new RouteMap("TabOne", View: views.FindByView<HotReloadTabContentPage>(), IsDefault: true),
					new RouteMap("TabTwo", View: views.FindByView<HotReloadTabContentPage>()),
				};

				if (HotReloadRouteRegistration.IncludeTabThree())
				{
					nested.Add(new RouteMap("TabThree", View: views.FindByView<HotReloadTabContentPage>()));
				}

				routes.Register(
					new RouteMap("", Nested: new RouteMap[]
					{
						new RouteMap(
							"HotReloadTabBarLateAddPage",
							View: views.FindByView<HotReloadTabBarLateAddPage>(),
							IsDefault: true,
							Nested: nested.ToArray()),
					}));
			},
			"HotReloadTabBarLateAddPage",
			ct);

	#endregion

	#region Infrastructure

	private sealed class TabBarTestApp : IAsyncDisposable
	{
		private readonly IHost _host;

		public TabBarTestApp(
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

	private static async Task<global::Uno.Extensions.Navigation.INavigator> WaitForTabBarNavigatorAsync(
		TabBar tabBar,
		TimeSpan timeout,
		CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (tabBar.Navigator() is { } nav)
			{
				return nav;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"TabBar's navigator (TabBarNavigator) did not become available within {timeout.TotalSeconds:F0}s.");
	}

	private static async Task<HotReloadTabBarVm> WaitForTabContentVmAsync(
		Grid contentGrid,
		string regionName,
		TimeSpan timeout,
		CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			var vm = FindTabContentVm(contentGrid, regionName);
			if (vm is not null)
			{
				return vm;
			}
			await Task.Delay(50, ct);
		}

		var children = string.Join(", ", contentGrid.Children
			.OfType<FrameworkElement>()
			.Select(c => $"{c.GetType().Name}[Region.Name='{Uno.Extensions.Navigation.UI.Region.GetName(c)}']"));
		throw new TimeoutException(
			$"Tab '{regionName}' did not populate a HotReloadTabContentPage within {timeout.TotalSeconds:F0}s. " +
			$"ContentGrid children: [{children}].");
	}

	private static HotReloadTabBarVm? FindTabContentVm(Grid contentGrid, string regionName)
	{
		var regionView = contentGrid.Children
			.OfType<FrameworkElement>()
			.FirstOrDefault(c => Uno.Extensions.Navigation.UI.Region.GetName(c) == regionName);
		if (regionView is FrameView fv &&
			fv.FindName("NavigationFrame") is Frame frame &&
			frame.Content is HotReloadTabContentPage page &&
			page.DataContext is HotReloadTabBarVm vm)
		{
			return vm;
		}
		return null;
	}

	/// <summary>
	/// Polls until the tab region has content matching the expected page type.
	/// Unlike <see cref="WaitForTabContentVmAsync"/> which looks for a specific VM,
	/// this checks only that a page of the specified type has been created — useful
	/// for auto-resolved pages that may not have a bound ViewModel.
	/// </summary>
	private static async Task<Page?> WaitForTabRegionContentAsync(
		Grid contentGrid,
		string regionName,
		Type expectedPageType,
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
				frame.Content is Page page &&
				expectedPageType.IsAssignableFrom(page.GetType()))
			{
				return page;
			}
			await Task.Delay(50, ct);
		}

		return null;
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

	/// <summary>
	/// Polls until the current page matches a content condition.
	/// Unlike <see cref="WaitForPageReplacementAsync{TPage}"/> which checks only
	/// for a different object reference, this waits for the page to have specific
	/// content — avoiding false positives from stale replacements caused by a
	/// prior test's async file revert on the same .xaml.
	/// </summary>
	private static async Task<TPage> WaitForPageMatchingAsync<TPage>(
		ContentControl root,
		Func<TPage, bool> condition,
		TimeSpan timeout,
		CancellationToken ct) where TPage : class
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			var current = ResolveCurrentPage<TPage>(root);
			if (current is not null && condition(current))
			{
				return current;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"No {typeof(TPage).Name} matching the expected content appeared within {timeout.TotalSeconds:F0}s.");
	}

	/// <summary>
	/// Polls until a specific tab region is confirmed absent from the content grid.
	/// </summary>
	private static async Task WaitForTabAbsentAsync(
		Grid contentGrid,
		string regionName,
		TimeSpan timeout,
		CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (FindTabContentVm(contentGrid, regionName) is null)
			{
				return;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"Tab '{regionName}' was still present after {timeout.TotalSeconds:F0}s.");
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

	#endregion
}
#endif
