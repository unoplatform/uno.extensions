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
	/// <summary>
	/// A 3-tab TabBar with IsDefault on TabOne should show TabOne's content
	/// immediately after navigation completes. Verifies the content grid
	/// has children (FrameView for the default tab).
	/// </summary>
	[TestMethod]
	public async Task When_ThreeTabBarWithDefaultRoute_Then_DefaultTabContentVisible(CancellationToken ct)
	{
		await using var app = await SetupTabBarAppAsync(
			registerViewsAndRoutes: (views, routes) =>
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
			},
			initialRoute: "HotReloadTabBarThreeTabPage",
			ct);

		var hostPage = await WaitForHostPageAsync<HotReloadTabBarThreeTabPage>(app.NavigationRoot, TimeSpan.FromSeconds(30), ct);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadTabBarThreeTabPage");

		// The content grid should have at least one child (the FrameView for TabOne).
		await UIHelper.WaitFor(() => hostPage!.ContentGrid.Children.Count > 0, ct);

		hostPage!.ContentGrid.Children.Count.Should().BeGreaterThan(0,
			"Default tab content should be visible — the content grid should have a FrameView for TabOne");

		// Verify the default tab's FrameView exists.
		var defaultTabFrameView = hostPage.ContentGrid.Children
			.OfType<FrameView>()
			.FirstOrDefault(fv => Region.GetName(fv) == "TabOne" || fv.Name == "TabOne");
		defaultTabFrameView.Should().NotBeNull(
			"A FrameView for TabOne should exist in the content grid");
	}

	/// <summary>
	/// After initial load with default tab, switching to another tab should work.
	/// This verifies the full region tree is wired up, not just the default tab.
	/// </summary>
	[TestMethod]
	public async Task When_DefaultTabLoaded_Then_SwitchToOtherTabWorks(CancellationToken ct)
	{
		await using var app = await SetupTabBarAppAsync(
			registerViewsAndRoutes: (views, routes) =>
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
			},
			initialRoute: "HotReloadTabBarThreeTabPage",
			ct);

		var hostPage = await WaitForHostPageAsync<HotReloadTabBarThreeTabPage>(app.NavigationRoot, TimeSpan.FromSeconds(30), ct);
		hostPage.Should().NotBeNull();

		// Wait for default tab to load.
		await UIHelper.WaitFor(() => hostPage!.ContentGrid.Children.Count > 0, ct);

		// Get the TabBar's navigator (the one that knows about tab routes).
		var tabBarNav = await WaitForTabBarNavigatorAsync(hostPage!.TabBar, TimeSpan.FromSeconds(30), ct);
		tabBarNav.Should().NotBeNull();

		// Navigate to TabTwo via the TabBar navigator.
		await tabBarNav.NavigateRouteAsync(hostPage, "TabTwo");

		// Wait for the content to change — the active FrameView region should switch.
		await UIHelper.WaitFor(() =>
		{
			var frames = hostPage.ContentGrid.Children.OfType<FrameView>().ToList();
			return frames.Any(fv => Region.GetName(fv) == "TabTwo");
		}, ct);
	}

	/// <summary>
	/// A 2-tab XAML-defined TabBar page should show default tab content on initial load.
	/// Uses the XAML page (HotReloadTabBarXamlPage) to test the XAML-driven region attachment path.
	/// </summary>
	[TestMethod]
	public async Task When_XamlTabBarWithDefaultRoute_Then_DefaultTabContentVisible(CancellationToken ct)
	{
		await using var app = await SetupTabBarAppAsync(
			registerViewsAndRoutes: (views, routes) =>
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
			},
			initialRoute: "HotReloadTabBarXamlPage",
			ct);

		var hostPage = await WaitForHostPageAsync<HotReloadTabBarXamlPage>(app.NavigationRoot, TimeSpan.FromSeconds(30), ct);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadTabBarXamlPage");

		// Wait for the content grid to have content.
		await UIHelper.WaitFor(() => hostPage!.ContentGrid.Children.Count > 0, ct);

		hostPage!.ContentGrid.Children.Count.Should().BeGreaterThan(0,
			"Default tab content should be visible in XAML-defined TabBar layout");
	}

	#region Setup & Helpers

	/// <summary>
	/// Boots an Uno host with toolkit navigation, hosts it in the runtime-tests engine's
	/// already-displayed test window, and navigates to <paramref name="initialRoute"/>.
	/// Disposal stops the host and restores the window's original content.
	/// </summary>
	private static async Task<TabBarTestApp> SetupTabBarAppAsync(
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
					.CreateDefaultBuilder(typeof(Given_TabBarInitialNavigation).Assembly)
					.UseToolkitNavigation()
					.UseNavigation(viewRouteBuilder: registerViewsAndRoutes)
					.Build(),
				navigationRoot: navigationRoot,
				initialRoute: initialRoute);

			return new TabBarTestApp(navigationRoot, host);
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

	private sealed class TabBarTestApp : IAsyncDisposable
	{
		private readonly IHost _host;

		public TabBarTestApp(ContentControl navigationRoot, IHost host)
		{
			NavigationRoot = navigationRoot;
			_host = host;
		}

		public ContentControl NavigationRoot { get; }

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

	private static async Task<TPage?> WaitForHostPageAsync<TPage>(
		ContentControl root,
		TimeSpan timeout,
		CancellationToken ct) where TPage : class
	{
		TPage? result = null;
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			result = ResolveCurrentPage<TPage>(root);
			if (result is not null)
			{
				return result;
			}
			await Task.Delay(50, ct);
		}

		throw new TimeoutException(
			$"{typeof(TPage).Name} did not appear within {timeout.TotalSeconds:F0}s. " +
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

	private static async Task<INavigator> WaitForTabBarNavigatorAsync(
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
			$"TabBar's navigator did not become available within {timeout.TotalSeconds:F0}s.");
	}

	#endregion
}
