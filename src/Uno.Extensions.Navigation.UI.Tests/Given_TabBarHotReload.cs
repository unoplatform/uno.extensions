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

	/// <summary>
	/// Proves hot-reload works across TabBar-driven region navigation. The host page
	/// <see cref="HotReloadTabBarPage"/> contains a TabBar with Region.Attached and two
	/// TabBarItems (TabOne, TabTwo). Each tab's content resolves to
	/// <see cref="HotReloadTabContentPage"/> with a <see cref="HotReloadTabBarVm"/> that reads
	/// <see cref="HotReloadTabBarTarget.GetValue"/> on every access. After the HR delta flips
	/// the target from "original" to "updated", switching to TabTwo lands on a VM that
	/// reflects the new value.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_SwitchTabAfterUpdate_Then_SelectedTabReflectsUpdate(CancellationToken ct)
	{
		await using var app = await SetupTabBarAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadTabBarPage");

		// TabOne is the IsDefault nested route — wait for it to materialize.
		var tabOneVm = await WaitForTabContentVmAsync(
			hostPage!.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVm.DisplayedValue.Should().Be("original",
			"TabOne's VM should read the pre-HR method body");

		// HR: flip the target's helper method. Disposal reverts the file on scope exit.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTabBarTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Switch to TabTwo via the TabBar's own navigator.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			hostPage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabTwo");

		var tabTwoVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tabTwoVm.DisplayedValue.Should().Be("updated",
			"TabTwo's VM should read the post-HR method body");
	}

	/// <summary>
	/// Complementary to <see cref="When_SwitchTabAfterUpdate_Then_SelectedTabReflectsUpdate"/>:
	/// applies HR while viewing TabTwo, then switches back to TabOne. Because
	/// <see cref="HotReloadTabBarVm.DisplayedValue"/> re-reads the HR'd method on every access,
	/// even a previously-viewed tab's VM reflects the update once re-shown.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_UpdateWhileOnTabTwo_Then_SwitchBackToTabOneReflectsUpdate(CancellationToken ct)
	{
		await using var app = await SetupTabBarAppAsync(ct);

		var hostPage = ResolveCurrentPage<HotReloadTabBarPage>(app.NavigationRoot);
		hostPage.Should().NotBeNull("Frame should have navigated to HotReloadTabBarPage");

		// Wait for TabOne (IsDefault) to materialize.
		var tabOneVmBefore = await WaitForTabContentVmAsync(
			hostPage!.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVmBefore.DisplayedValue.Should().Be("original");

		// Switch to TabTwo before HR — verify baseline there too.
		var tabBarNavigator = await WaitForTabBarNavigatorAsync(
			hostPage.TabBar, TimeSpan.FromSeconds(30), ct);
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabTwo");

		var tabTwoVm = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabTwo", TimeSpan.FromSeconds(30), ct);
		tabTwoVm.DisplayedValue.Should().Be("original",
			"TabTwo (pre-HR) should read 'original'");

		// Apply HR while viewing TabTwo.
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTabBarTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// Switch back to TabOne — re-showing it exercises the reused-VM path.
		await tabBarNavigator.NavigateRouteAsync(hostPage, "TabOne");

		var tabOneVmAfter = await WaitForTabContentVmAsync(
			hostPage.ContentGrid, "TabOne", TimeSpan.FromSeconds(30), ct);
		tabOneVmAfter.DisplayedValue.Should().Be("updated",
			"TabOne's VM should read the post-HR method body after switching back");
	}

	#region Helpers

	/// <summary>
	/// Boots an Uno host with Toolkit navigation (registers <c>TabBarNavigator</c>),
	/// hosts it in the runtime-tests engine's test window, and navigates to the
	/// <see cref="HotReloadTabBarPage"/>.
	/// </summary>
	private static async Task<TabBarTestApp> SetupTabBarAppAsync(CancellationToken ct)
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
					.UseNavigation(viewRouteBuilder: (views, routes) =>
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
					})
					.Build(),
				navigationRoot: navigationRoot,
				initialRoute: "HotReloadTabBarPage");

			var frameNav = await WaitForFrameNavigatorAsync(navigationRoot, TimeSpan.FromSeconds(30), ct);
			await WaitForRouteAsync(navigationRoot, frameNav, "HotReloadTabBarPage", TimeSpan.FromSeconds(30), ct);

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
			await Task.Delay(50, ct);
		}

		var children = string.Join(", ", contentGrid.Children
			.OfType<FrameworkElement>()
			.Select(c => $"{c.GetType().Name}[Region.Name='{Uno.Extensions.Navigation.UI.Region.GetName(c)}']"));
		throw new TimeoutException(
			$"Tab '{regionName}' did not populate a HotReloadTabContentPage within {timeout.TotalSeconds:F0}s. " +
			$"ContentGrid children: [{children}].");
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
