using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.UI.Controls;
using Uno.Extensions.Navigation.UI.Tests.Pages.NavViewShell;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Reproduces the "region loads before its service provider is discoverable" seam: a hosting
/// environment that re-grafts app content can fire an attached region's Loading/Loaded while the
/// upward provider walk still finds nothing, so AssignParent warns "Unable to find service provider
/// for root navigator" and gives up. The provider becomes discoverable moments later, but
/// Loading/Loaded never re-fire — without a retry the region stays permanently detached and every
/// request targeting its routes is parked with no region to resume them (dead default tab).
/// <see cref="NavigationRegion"/>'s bounded AssignParent retry is the behavior under test.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_RegionAssignParentRetry
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	[TestMethod]
	public async Task When_ServiceProviderDiscoverableOnlyAfterLoad_Then_RegionRecoversAndNavigates(CancellationToken ct)
	{
		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		UnitTestsUIContentHelper.SaveOriginalContent();
		IHost? host = null;
		try
		{
			// A regular navigation host with a single default route.
			host = UnoHost
				.CreateDefaultBuilder(typeof(Given_RegionAssignParentRetry).Assembly)
				.UseNavigation(viewRouteBuilder: (views, routes) =>
				{
					views.Register(new ViewMap<NavHomePage>());
					routes.Register(new RouteMap("NavHome", View: views.FindByView<NavHomePage>(), IsDefault: true));
				})
				.Build();
			await Task.Run(() => host.StartAsync());
			var services = await host.Services.RegisterWindowAsync(window);

			// The region element loads under a root that has NO service provider attached yet —
			// the condition a re-grafting host produces at Loaded time.
			var regionHost = new ContentControl
			{
				HorizontalContentAlignment = HorizontalAlignment.Stretch,
				VerticalContentAlignment = VerticalAlignment.Stretch,
			};
			regionHost.SetAttached(true); // creates the NavigationRegion; Loading/Loaded still pending
			var root = new Grid();
			root.Children.Add(regionHost);
			window.Content = root;

			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				await WaitForAsync(() => regionHost.IsLoaded, cts.Token);
			}

			// Scenario precondition: AssignParent already ran (synchronously within Loading/Loaded)
			// and gave up — the region has no parent, no services, and no navigator.
			var region = regionHost.GetInstance();
			region.Should().NotBeNull();
			region!.Navigator().Should().BeNull("the region must have failed to wire while no provider was discoverable (scenario precondition)");

			// The seam: the provider becomes discoverable only now, after Loaded has come and gone.
			root.SetServiceProvider(services);

			// The region must recover via the bounded AssignParent retry and run its initial
			// navigation to the default route.
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
			{
				cts.CancelAfter(Timeout);
				try
				{
					await WaitForAsync(() => ResolveContent<NavHomePage>(regionHost) is not null, cts.Token);
				}
				catch (OperationCanceledException) when (!ct.IsCancellationRequested)
				{
					Assert.Fail(
						$"Region never recovered after the service provider became discoverable " +
						$"(navigator: {(region.Navigator() is null ? "null" : "created")}, content: {regionHost.Content?.GetType().Name ?? "null"}). " +
						"AssignParent gave up at Loaded and nothing retried.");
				}
			}

			region.Navigator().Should().NotBeNull();
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
			if (host is not null)
			{
				await Task.Run(() => host.StopAsync());
			}
		}
	}

	// UIHelper.WaitFor has a short internal budget (1s undebugged) and throws TimeoutException;
	// keep polling until the caller's token expires so waits get the full test timeout.
	private static async Task WaitForAsync(Func<bool> predicate, CancellationToken ct)
	{
		while (true)
		{
			ct.ThrowIfCancellationRequested();
			try
			{
				await UIHelper.WaitFor(predicate, ct);
				return;
			}
			catch (TimeoutException)
			{
			}
		}
	}

	// The root navigator hosts a Page either directly or wrapped as FrameView(Frame(page)),
	// depending on navigator selection — accept both (same duality as ResolveShellPage).
	private static TPage? ResolveContent<TPage>(ContentControl root)
		where TPage : Page
	{
		if (root.Content is FrameView fv &&
			fv.Content is Frame frame &&
			frame.Content is TPage page)
		{
			return page;
		}

		return root.Content as TPage;
	}
}
