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

		// Give the redirect a moment to settle, then verify TabThree content is absent.
		await Task.Delay(500, ct);
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

		// Give the visual tree time to settle after XAML HR.
		await Task.Delay(1000, ct);

		// XAML HR replaces the page — re-resolve to check the NEW page's state.
		var activePage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot)!;

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

		await Task.Delay(1000, ct);

		// XAML HR replaces the page instance — re-resolve to get the new one.
		var activePage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot)!;

		// Verify the Region.Name DP was updated on the new page's TabBarItems.
		var regionNames = activePage.TabBar.Items.OfType<FrameworkElement>()
			.Select(i => Uno.Extensions.Navigation.UI.Region.GetName(i))
			.ToList();
		regionNames.Should().Contain("TabTwoRenamed",
			"XAML HR should produce TabBarItems with the renamed Region.Name");

		// Navigate to the renamed route using the new page's navigator.
		var activeNavigator = await WaitForTabBarNavigatorAsync(
			activePage.TabBar, TimeSpan.FromSeconds(30), ct);
		await activeNavigator.NavigateRouteAsync(activePage, "TabTwoRenamed");
		await Task.Delay(500, ct);

		var renamedVm = FindTabContentVm(activePage.ContentGrid, "TabTwoRenamed");
		renamedVm.Should().NotBeNull(
			"Navigation should resolve the renamed Region.Name after XAML HR");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 9. TabBarItem added via XAML HR
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR adds a third <c>TabBarItem</c> with Region.Name="TabThree".
	/// The route is pre-registered so the SelectorNavigator can navigate to it.
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

		await Task.Delay(1000, ct);

		// XAML HR replaces the page instance — re-resolve to get the new one.
		var activePage = ResolveCurrentPage<HotReloadTabBarXamlPage>(app.NavigationRoot)!;

		// TabBar should now have 3 items on the replaced page.
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
		await Task.Delay(200, ct);

		// Phase 1: remove the Command binding.
		var revert = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/Pages/HotReloadTabBarCommandPage.xaml",
			"""Content="Navigate" Command="{Binding TestCommand}" """,
			"""Content="Navigate" """,
			ct);

		// Tab switching should still work without the Command.
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabTwo");
		await Task.Delay(200, ct);

		FindTabContentVm(hostPage.ContentGrid, "TabTwo").Should().NotBeNull(
			"Tab switching should work after Command binding removal");

		// Phase 2: file revert re-adds the Command binding via XAML HR.
		await revert.DisposeAsync();
		await Task.Delay(1000, ct);

		// #2912: tab switching should still work after Command is restored.
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabOne");
		await Task.Delay(500, ct);

		FindTabContentVm(hostPage.ContentGrid, "TabOne").Should().NotBeNull(
			"Tab switching should work after Command binding is restored (#2912)");
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

	#endregion
}
#endif
