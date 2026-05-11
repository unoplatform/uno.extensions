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
using Uno.Extensions.Navigation.UI.Controls;
using Uno.Extensions.Navigation.UI.Tests.Pages;
using Uno.Extensions.Navigation.UI.Tests.Pages.TabNav;
using Uno.Toolkit.UI;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Tests that the default tab content is visible after initial navigation
/// with a TabBar-based layout. This covers the race condition where child
/// NavigationRegions may not be attached when the initial navigation fires,
/// causing the content area to remain blank (the TabBar itself renders but
/// no page content is shown).
///
/// Reported in Studio Live where the ALC in-process hosting delays Loaded
/// events on the dispatcher, but the test is valuable as a baseline
/// non-HR regression guard for TabBar initial load.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_TabBarInitialNavigation
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

	/// <summary>
	/// A 3-tab TabBar with IsDefault on TabOne should show TabOne's content
	/// immediately after navigation completes. Verifies the content grid
	/// has children (FrameView for the default tab).
	/// </summary>
	[TestMethod]
	public async Task When_ThreeTabBarWithDefaultRoute_Then_DefaultTabContentVisible()
	{
		var window = new Window();

		IHost? host = null;
		host = await window.InitializeNavigationAsync(
			buildHost: async () => UnoHost
				.CreateDefaultBuilder(typeof(Given_TabBarInitialNavigation).Assembly)
				.UseToolkitNavigation()
				.UseNavigation(
					viewRouteBuilder: (views, routes) =>
					{
						views.Register(
							new ViewMap<HotReloadTabBarThreeTabPage>(),
							new ViewMap<TabAPage>(),
							new ViewMap<TabBPage>(),
							new ViewMap<ForwardNavPage>());

						routes.Register(
							new RouteMap("", Nested: new RouteMap[]
							{
								new RouteMap(
									"HotReloadTabBarThreeTabPage",
									View: views.FindByView<HotReloadTabBarThreeTabPage>(),
									IsDefault: true,
									Nested: new RouteMap[]
									{
										new RouteMap("TabOne", View: views.FindByView<TabAPage>(), IsDefault: true),
										new RouteMap("TabTwo", View: views.FindByView<TabBPage>()),
										new RouteMap("TabThree", View: views.FindByView<ForwardNavPage>()),
									}),
							}));
					})
				.Build(),
			initialRoute: "HotReloadTabBarThreeTabPage");

		try
		{
			var root = window.Content as ContentControl;
			root.Should().NotBeNull();

			// Wait for the host page to load.
			using var cts = new CancellationTokenSource(Timeout);
			HotReloadTabBarThreeTabPage? hostPage = null;
			await UIHelper.WaitFor(() =>
			{
				hostPage = GetHostPage(root!);
				return hostPage is not null;
			}, cts.Token);

			hostPage.Should().NotBeNull("Frame should have navigated to HotReloadTabBarThreeTabPage");

			// The content grid should have at least one child (the FrameView for TabOne).
			await UIHelper.WaitFor(() =>
			{
				return hostPage!.ContentGrid.Children.Count > 0;
			}, cts.Token);

			hostPage!.ContentGrid.Children.Count.Should().BeGreaterThan(0,
				"Default tab content should be visible — the content grid should have a FrameView for TabOne");

			// Verify the default tab's FrameView exists and contains a TabAPage.
			var defaultTabFrameView = hostPage.ContentGrid.Children
				.OfType<FrameView>()
				.FirstOrDefault(fv => Region.GetName(fv) == "TabOne" || fv.Name == "TabOne");
			defaultTabFrameView.Should().NotBeNull(
				"A FrameView for TabOne should exist in the content grid");

			// The navigator should have a route pointing to the default tab.
			var nav = root!.Navigator();
			nav.Should().NotBeNull();
			nav!.Route.Should().NotBeNull("Navigator should have an active route");
		}
		finally
		{
			await host!.StopAsync();
		}
	}

	/// <summary>
	/// After initial load with default tab, switching to another tab should work.
	/// This verifies the full region tree is wired up, not just the default tab.
	/// </summary>
	[TestMethod]
	public async Task When_DefaultTabLoaded_Then_SwitchToOtherTabWorks()
	{
		var window = new Window();

		IHost? host = null;
		host = await window.InitializeNavigationAsync(
			buildHost: async () => UnoHost
				.CreateDefaultBuilder(typeof(Given_TabBarInitialNavigation).Assembly)
				.UseToolkitNavigation()
				.UseNavigation(
					viewRouteBuilder: (views, routes) =>
					{
						views.Register(
							new ViewMap<HotReloadTabBarThreeTabPage>(),
							new ViewMap<TabAPage>(),
							new ViewMap<TabBPage>(),
							new ViewMap<ForwardNavPage>());

						routes.Register(
							new RouteMap("", Nested: new RouteMap[]
							{
								new RouteMap(
									"HotReloadTabBarThreeTabPage",
									View: views.FindByView<HotReloadTabBarThreeTabPage>(),
									IsDefault: true,
									Nested: new RouteMap[]
									{
										new RouteMap("TabOne", View: views.FindByView<TabAPage>(), IsDefault: true),
										new RouteMap("TabTwo", View: views.FindByView<TabBPage>()),
										new RouteMap("TabThree", View: views.FindByView<ForwardNavPage>()),
									}),
							}));
					})
				.Build(),
			initialRoute: "HotReloadTabBarThreeTabPage");

		try
		{
			var root = window.Content as ContentControl;
			root.Should().NotBeNull();

			using var cts = new CancellationTokenSource(Timeout);
			HotReloadTabBarThreeTabPage? hostPage = null;
			await UIHelper.WaitFor(() =>
			{
				hostPage = GetHostPage(root!);
				return hostPage is not null;
			}, cts.Token);

			hostPage.Should().NotBeNull();

			// Wait for default tab to load.
			await UIHelper.WaitFor(() => hostPage!.ContentGrid.Children.Count > 0, cts.Token);

			// Get the TabBar's navigator (the one that knows about tab routes).
			INavigator? tabBarNav = null;
			await UIHelper.WaitFor(() =>
			{
				tabBarNav = hostPage!.TabBar.Navigator();
				return tabBarNav is not null;
			}, cts.Token);
			tabBarNav.Should().NotBeNull();

			// Navigate to TabTwo via the TabBar navigator.
			await tabBarNav!.NavigateRouteAsync(hostPage!, "TabTwo");

			// Wait for the content to change — the active FrameView region should switch.
			await UIHelper.WaitFor(() =>
			{
				var frames = hostPage!.ContentGrid.Children.OfType<FrameView>().ToList();
				return frames.Any(fv =>
				{
					var name = Region.GetName(fv);
					return name == "TabTwo";
				});
			}, cts.Token);
		}
		finally
		{
			await host!.StopAsync();
		}
	}

	/// <summary>
	/// A 2-tab XAML-defined TabBar page should show default tab content on initial load.
	/// Uses the XAML page (HotReloadTabBarXamlPage) to test the XAML-driven region attachment path.
	/// </summary>
	[TestMethod]
	public async Task When_XamlTabBarWithDefaultRoute_Then_DefaultTabContentVisible()
	{
		var window = new Window();

		IHost? host = null;
		host = await window.InitializeNavigationAsync(
			buildHost: async () => UnoHost
				.CreateDefaultBuilder(typeof(Given_TabBarInitialNavigation).Assembly)
				.UseToolkitNavigation()
				.UseNavigation(
					viewRouteBuilder: (views, routes) =>
					{
						views.Register(
							new ViewMap<HotReloadTabBarXamlPage>(),
							new ViewMap<TabAPage>(),
							new ViewMap<TabBPage>());

						routes.Register(
							new RouteMap("", Nested: new RouteMap[]
							{
								new RouteMap(
									"HotReloadTabBarXamlPage",
									View: views.FindByView<HotReloadTabBarXamlPage>(),
									IsDefault: true,
									Nested: new RouteMap[]
									{
										new RouteMap("TabOne", View: views.FindByView<TabAPage>(), IsDefault: true),
										new RouteMap("TabTwo", View: views.FindByView<TabBPage>()),
									}),
							}));
					})
				.Build(),
			initialRoute: "HotReloadTabBarXamlPage");

		try
		{
			var root = window.Content as ContentControl;
			root.Should().NotBeNull();

			using var cts = new CancellationTokenSource(Timeout);
			HotReloadTabBarXamlPage? hostPage = null;
			await UIHelper.WaitFor(() =>
			{
				hostPage = GetXamlHostPage(root!);
				return hostPage is not null;
			}, cts.Token);

			hostPage.Should().NotBeNull("Frame should have navigated to HotReloadTabBarXamlPage");

			// Wait for the content grid to have content.
			await UIHelper.WaitFor(() => hostPage!.ContentGrid.Children.Count > 0, cts.Token);

			hostPage!.ContentGrid.Children.Count.Should().BeGreaterThan(0,
				"Default tab content should be visible in XAML-defined TabBar layout");
		}
		finally
		{
			await host!.StopAsync();
		}
	}

	#region Helpers

	private static HotReloadTabBarThreeTabPage? GetHostPage(ContentControl root)
	{
		if (root.Content is FrameView fv &&
			fv.Content is Frame frame &&
			frame.Content is HotReloadTabBarThreeTabPage page)
		{
			return page;
		}
		return root.Content as HotReloadTabBarThreeTabPage;
	}

	private static HotReloadTabBarXamlPage? GetXamlHostPage(ContentControl root)
	{
		if (root.Content is FrameView fv &&
			fv.Content is Frame frame &&
			frame.Content is HotReloadTabBarXamlPage page)
		{
			return page;
		}
		return root.Content as HotReloadTabBarXamlPage;
	}

	#endregion
}
