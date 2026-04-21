#if DEBUG // Hot-reload tests are only relevant in debug configuration
using System;
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
