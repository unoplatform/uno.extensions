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
		// Host the navigation region inside the runtime-tests engine's already-displayed
		// test window. Creating a fresh `new Window()` in RunsInSecondaryApp mode produces
		// an un-composited window whose Loaded/Activate events never fire, which in turn
		// prevents initial navigation from running — the symptom is a black secondary app.
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

		try
		{
			IHost? host = null;
			host = await window.InitializeNavigationAsync(
				buildHost: async () =>
				{
					var h = UnoHost
						.CreateDefaultBuilder(typeof(Given_HotReload).Assembly)
						.UseNavigation(
							viewRouteBuilder: (views, routes) =>
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
							})
						.Build();
					return h;
				},
				navigationRoot: navigationRoot,
				// Navigate directly to the target page rather than relying on root "" → IsDefault
				// descent. Other tests in this project (Given_ChainedGetDataAsync, Given_RouteNotifier)
				// follow this pattern; descending from the empty root requires a nested Region.Attached
				// ContentControl, which we don't have.
				initialRoute: "HotReloadPageOne");

			try
			{
				// When navigating a Page into a ContentControl root, ContentControlNavigator wraps
				// the Page in a FrameView (see ContentControlNavigator.Show). The Page ends up in
				// the FrameView's inner Frame, and the Frame's navigator is what tracks the route.
				// So we look at the FrameView's Navigator, not the root ContentControl's.
				var frameNav = await WaitForFrameNavigatorAsync(navigationRoot, TimeSpan.FromSeconds(30), ct);

				await WaitForRouteAsync(navigationRoot, frameNav, "HotReloadPageOne", TimeSpan.FromSeconds(30), ct);

				var page1 = ResolveCurrentPage<HotReloadPageOne>(navigationRoot);
				page1.Should().NotBeNull("Frame should have navigated to HotReloadPageOne");
				page1!.DisplayedValue.Should().Be("original");

				// Apply the hot-reload source change. Disposal on scope-exit reverts the file.
				await using var _ = await HotReloadHelper.UpdateSourceFile(
					"../../Uno.Extensions.Navigation.UI.Tests/HotReloadTarget.cs",
					"""return "original";""",
					"""return "updated";""",
					ct);

				// Navigate to a fresh page — its constructor must observe the updated method body.
				await frameNav.NavigateRouteAsync(this, "HotReloadPageTwo");

				await WaitForRouteAsync(navigationRoot, frameNav, "HotReloadPageTwo", TimeSpan.FromSeconds(30), ct);

				var page2 = ResolveCurrentPage<HotReloadPageTwo>(navigationRoot);
				page2.Should().NotBeNull("Frame should have navigated to HotReloadPageTwo");
				page2!.DisplayedValue.Should().Be("updated");
			}
			finally
			{
				if (host is not null)
				{
					await host.StopAsync();
				}
			}
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
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
