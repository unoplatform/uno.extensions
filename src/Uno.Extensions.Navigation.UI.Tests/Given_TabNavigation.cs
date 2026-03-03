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
using Uno.Extensions.Navigation.Navigators;
using Uno.Extensions.Navigation.UI;
using Uno.Extensions.Navigation.UI.Controls;
using Uno.Extensions.Navigation.UI.Tests.Pages.TabNav;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Tests for the tab navigation fix (IsSiblingTabRoute).
///
/// Bug: When navigating from one tab's FrameNavigator to a sibling tab route,
/// the FrameNavigator would push the target page forward (frame push) instead of
/// delegating to the parent PanelVisibilityNavigator for a tab switch.
///
/// Fix: Navigator.IsSiblingTabRoute detects that the target route matches a sibling
/// SelectorNavigator's item and skips the self-redirect in
/// RedirectForImplicitForwardNavigation, allowing the parent to handle the tab switch.
///
/// Route structure:
///   "" (root)
///     "TabbedMain" (TabbedMainPage) [IsDefault]
///       "TabA" (TabAPage) [IsDefault]
///       "TabB" (TabBPage)
///       "ForwardNav" (ForwardNavPage) — nested but NOT a tab
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_TabNavigation
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	private async Task<(IHost Host, INavigator Navigator, ContentControl Root)> SetupTabNavigationAsync()
	{
		var window = new Window();

		IHost? host = null;
		host = await window.InitializeNavigationAsync(
			buildHost: async () =>
			{
				var h = UnoHost
					.CreateDefaultBuilder(typeof(Given_TabNavigation).Assembly)
					.UseNavigation(
						viewRouteBuilder: (views, routes) =>
						{
							views.Register(
								new ViewMap<TabbedMainPage>(),
								new ViewMap<TabAPage>(),
								new ViewMap<TabBPage>(),
								new ViewMap<ForwardNavPage>());

							routes.Register(
								new RouteMap("", Nested: new RouteMap[]
								{
									new RouteMap("TabbedMain", View: views.FindByView<TabbedMainPage>(), IsDefault: true,
										Nested: new RouteMap[]
										{
											new RouteMap("TabA", View: views.FindByView<TabAPage>(), IsDefault: true),
											new RouteMap("TabB", View: views.FindByView<TabBPage>()),
											new RouteMap("ForwardNav", View: views.FindByView<ForwardNavPage>()),
										}),
								}));
						})
					.Build();
				return h;
			},
			initialRoute: "TabbedMain");

		var root = (ContentControl)window.Content!;
		var navigator = root.Navigator()!;

		return (host, navigator, root);
	}

	/// <summary>
	/// Finds the FrameView for a given tab name inside the TabbedMainPage's content grid.
	/// Returns null if the FrameView hasn't been created yet (lazy tabs).
	/// </summary>
	private FrameView? FindTabFrameView(TabbedMainPage tabbedPage, string tabName)
	{
		return tabbedPage.ContentGrid.Children
			.OfType<FrameView>()
			.FirstOrDefault(fv => Region.GetName(fv) == tabName || fv.Name == tabName);
	}

	/// <summary>
	/// Gets the TabbedMainPage from the navigation host's visual tree.
	/// The root ContentControl wraps a FrameView whose Frame contains TabbedMainPage.
	/// </summary>
	private TabbedMainPage? GetTabbedMainPage(ContentControl root)
	{
		// The ContentControlNavigator wraps pages in a FrameView.
		// ContentControl.Content → FrameView → Frame.Content → TabbedMainPage
		if (root.Content is FrameView fv &&
			fv.Content is Frame frame &&
			frame.Content is TabbedMainPage page)
		{
			return page;
		}

		// Alternative: direct content
		return root.Content as TabbedMainPage;
	}

	/// <summary>
	/// Navigating from within a tab's FrameNavigator to a sibling tab route should
	/// result in a tab switch (PanelVisibilityNavigator shows the target tab), NOT
	/// a frame push on the current tab's Frame.
	///
	/// This tests the lazy-tab path: TabB has never been visited, so no FrameView
	/// named "TabB" exists in the content grid. IsSiblingTabRoute must check the
	/// NavigationViewNavigator's items to determine it's a tab route.
	/// </summary>
	[TestMethod]
	public async Task When_NavigateToSiblingTab_Then_TabSwitchesInsteadOfFramePush()
	{
		var (host, navigator, root) = await SetupTabNavigationAsync();

		try
		{
			// Wait for the default tab (TabA) to load
			using var setupCts = new CancellationTokenSource(Timeout);
			TabbedMainPage? tabbedPage = null;
			await UIHelper.WaitFor(() =>
			{
				tabbedPage = GetTabbedMainPage(root);
				return tabbedPage is not null;
			}, setupCts.Token);

			tabbedPage.Should().NotBeNull("TabbedMainPage should be loaded");

			// Wait for TabA's FrameView to be created (default tab)
			FrameView? tabAFrameView = null;
			await UIHelper.WaitFor(() =>
			{
				tabAFrameView = FindTabFrameView(tabbedPage!, "TabA");
				return tabAFrameView is not null;
			}, setupCts.Token);

			tabAFrameView.Should().NotBeNull("TabA FrameView should be created as the default tab");

			// Get TabA's FrameNavigator — this is the navigator that HomeModel would use
			var tabANavigator = tabAFrameView!.Navigator;
			tabANavigator.Should().NotBeNull("TabA's FrameView should have a navigator");

			// Verify TabB's FrameView does NOT exist yet (testing the lazy path)
			var tabBFrameViewBefore = FindTabFrameView(tabbedPage!, "TabB");
			tabBFrameViewBefore.Should().BeNull(
				"TabB FrameView should not exist before first navigation (lazy creation)");

			// Act: Navigate to "TabB" from TabA's FrameNavigator
			// This is the exact pattern that HomeModel.CategorySearch() uses:
			//   _navigator.NavigateRouteAsync(this, "Search")
			await tabANavigator!.NavigateRouteAsync(tabbedPage!, "TabB");

			// Wait for TabB's FrameView to be created (tab switch)
			FrameView? tabBFrameView = null;
			using var navCts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(() =>
			{
				tabBFrameView = FindTabFrameView(tabbedPage!, "TabB");
				return tabBFrameView is not null;
			}, navCts.Token);

			// Assert: TabB FrameView was created (tab switch happened)
			tabBFrameView.Should().NotBeNull(
				"TabB FrameView should be created by PanelVisibilityNavigator when switching tabs");

			// Assert: TabA's Frame should NOT have any back stack entries.
			// If the bug existed, "TabB" would have been pushed onto TabA's Frame,
			// creating a back stack entry. With the fix, it's a tab switch, so no push.
			if (tabAFrameView!.Content is Frame tabAFrame)
			{
				tabAFrame.BackStackDepth.Should().Be(0,
					"TabA's Frame should have no back stack — the navigation should be a tab switch, not a frame push");
			}
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// Navigating from within a tab's FrameNavigator to a NON-tab route (one that
	/// has no matching NavigationViewItem) should be handled as a forward frame push
	/// on the current tab's Frame. This verifies that IsSiblingTabRoute correctly
	/// returns false for non-tab routes.
	/// </summary>
	[TestMethod]
	public async Task When_NavigateToNonTabRoute_Then_FramePush()
	{
		var (host, navigator, root) = await SetupTabNavigationAsync();

		try
		{
			// Wait for TabbedMainPage and TabA to load
			using var setupCts = new CancellationTokenSource(Timeout);
			TabbedMainPage? tabbedPage = null;
			await UIHelper.WaitFor(() =>
			{
				tabbedPage = GetTabbedMainPage(root);
				return tabbedPage is not null;
			}, setupCts.Token);

			FrameView? tabAFrameView = null;
			await UIHelper.WaitFor(() =>
			{
				tabAFrameView = FindTabFrameView(tabbedPage!, "TabA");
				return tabAFrameView is not null;
			}, setupCts.Token);

			var tabANavigator = tabAFrameView!.Navigator;
			tabANavigator.Should().NotBeNull("TabA's FrameView should have a navigator");

			// Act: Navigate to "ForwardNav" from TabA's FrameNavigator
			// "ForwardNav" has no matching NavigationViewItem, so it should NOT be treated as a tab.
			await tabANavigator!.NavigateRouteAsync(tabbedPage!, "ForwardNav");

			// Wait for the navigation to go through
			using var navCts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(() =>
			{
				if (tabAFrameView!.Content is Frame f)
				{
					return f.Content is ForwardNavPage;
				}
				return false;
			}, navCts.Token);

			// Assert: ForwardNavPage was pushed on TabA's Frame (not as a tab switch)
			if (tabAFrameView!.Content is Frame tabAFrame)
			{
				tabAFrame.Content.Should().BeOfType<ForwardNavPage>(
					"ForwardNavPage should be shown as a forward push on TabA's Frame");
				tabAFrame.BackStackDepth.Should().Be(1,
					"TabA's Frame should have one back stack entry (TabAPage was the previous page)");
			}
			else
			{
				Assert.Fail("TabA's FrameView should contain a Frame");
			}

			// Verify: No FrameView for "ForwardNav" was created in the content grid
			// (it's NOT a tab, so PanelVisibilityNavigator should NOT have created a FrameView)
			var forwardNavFrameView = FindTabFrameView(tabbedPage!, "ForwardNav");
			forwardNavFrameView.Should().BeNull(
				"ForwardNav should not have a FrameView — it's a frame push, not a tab");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	/// <summary>
	/// After visiting both tabs and then navigating to a previously-visited tab,
	/// the quick path in IsSiblingTabRoute (checking existing panel children) should
	/// correctly identify it as a tab route. Verifies both the lazy-creation path
	/// (first visit) and the existing-FrameView path (subsequent visit).
	/// </summary>
	[TestMethod]
	public async Task When_NavigateToAlreadyVisitedTab_Then_TabSwitchesViaQuickPath()
	{
		var (host, navigator, root) = await SetupTabNavigationAsync();

		try
		{
			// Wait for TabbedMainPage and TabA to load
			using var setupCts = new CancellationTokenSource(Timeout);
			TabbedMainPage? tabbedPage = null;
			await UIHelper.WaitFor(() =>
			{
				tabbedPage = GetTabbedMainPage(root);
				return tabbedPage is not null;
			}, setupCts.Token);

			FrameView? tabAFrameView = null;
			await UIHelper.WaitFor(() =>
			{
				tabAFrameView = FindTabFrameView(tabbedPage!, "TabA");
				return tabAFrameView is not null;
			}, setupCts.Token);

			var tabANavigator = tabAFrameView!.Navigator;

			// Step 1: Navigate to TabB (first time — lazy creation path)
			await tabANavigator!.NavigateRouteAsync(tabbedPage!, "TabB");

			// Wait for TabB to be created
			FrameView? tabBFrameView = null;
			using var navCts1 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(() =>
			{
				tabBFrameView = FindTabFrameView(tabbedPage!, "TabB");
				return tabBFrameView is not null;
			}, navCts1.Token);

			// Step 2: Navigate back to TabA from TabB
			var tabBNavigator = tabBFrameView!.Navigator;
			tabBNavigator.Should().NotBeNull("TabB's FrameView should have a navigator");
			await tabBNavigator!.NavigateRouteAsync(tabbedPage!, "TabA");

			// Wait for TabA to be shown again
			using var navCts2 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(() =>
			{
				// TabA's FrameView should still exist and be visible
				var fv = FindTabFrameView(tabbedPage!, "TabA");
				return fv is not null && fv.Visibility == Visibility.Visible;
			}, navCts2.Token);

			// Step 3: Navigate to TabB again (second time — existing FrameView quick path)
			// TabB's FrameView already exists in the content grid, so IsSiblingTabRoute
			// should match via the quick panel-children check.
			tabANavigator = tabAFrameView!.Navigator;
			await tabANavigator!.NavigateRouteAsync(tabbedPage!, "TabB");

			using var navCts3 = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(() =>
			{
				tabBFrameView = FindTabFrameView(tabbedPage!, "TabB");
				return tabBFrameView is not null && tabBFrameView.Visibility == Visibility.Visible;
			}, navCts3.Token);

			// Assert: Still a tab switch (no frame push on either tab)
			if (tabAFrameView!.Content is Frame tabAFrame)
			{
				tabAFrame.BackStackDepth.Should().Be(0,
					"TabA's Frame should have no back stack — all navigations were tab switches");
			}

			if (tabBFrameView!.Content is Frame tabBFrame)
			{
				tabBFrame.BackStackDepth.Should().Be(0,
					"TabB's Frame should have no back stack — all navigations were tab switches");
			}
		}
		finally
		{
			await host.StopAsync();
		}
	}
}
