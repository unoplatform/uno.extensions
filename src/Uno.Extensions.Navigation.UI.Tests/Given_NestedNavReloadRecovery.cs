using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation.UI.Tests.Pages;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Reproduces spec 006: the initial navigation dies silently when the hosting tree is
/// re-grafted (detached and re-attached) while the request is being forwarded to child
/// regions — the production case is Hot Design re-hosting an ALC-loaded app's content
/// moments after launch, leaving the shell Frame permanently empty (blank app).
///
/// The deterministic seam: the FrameView's inner Frame raises Loaded while it is still
/// empty (navigation only lands after the parent forwards the request), and the
/// child-forwarding continuation is queued behind that dispatch. Detaching the window
/// content inside Frame.Loaded therefore always lands between "child region attached"
/// and "request forwarded" — the same gap the in-vivo timeline shows
/// (Frame loaded t=1647, tree unloaded t=1900, request dropped, blank forever).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_NestedNavReloadRecovery
{
	private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(15);

	[TestMethod]
	public async Task When_ContentDetachedDuringInitialNavigation_Then_RouteResumesOnReattach()
	{
		var window = new Window();

		var initTask = window.InitializeNavigationAsync(
			buildHost: () => Task.FromResult(UnoHost
				.CreateDefaultBuilder(typeof(Given_NestedNavReloadRecovery).Assembly)
				.UseNavigation(
					viewRouteBuilder: (views, routes) =>
					{
						views.Register(
							new ViewMap<TestPageOne>());

						routes.Register(
							new RouteMap("", Nested: new RouteMap[]
							{
								new RouteMap("TestPageOne", View: views.FindByView<TestPageOne>(), IsDefault: true),
							}));
					})
				.Build()),
			// Explicit initial route, matching the production app shape (the generated
			// apps navigate initialRoute: "Main"): this drives the root ContentControl
			// navigator's Show() → FrameView → inner Frame region — the topology whose
			// mid-flight re-graft this test exercises. (Descending from "" to an
			// IsDefault leaf requires children to already be attached — see
			// HotReload.Spec.md "Initial route must be explicit".)
			initialRoute: "TestPageOne");

		var root = window.Content as ContentControl;
		root.Should().NotBeNull("Window.Content should be the navigation root ContentControl");

		IHost? host = null;
		try
		{
			// Wait for the initial navigation to create the FrameView's inner Frame,
			// and hook its Loaded before it fires (the Frame is created unloaded and
			// only enters the tree on a later tick).
			Frame? frame = null;
			var detachedDuringLoad = false;
			using (var cts = new CancellationTokenSource(TestTimeout))
			{
				await UIHelper.WaitFor(
					() =>
					{
						if (frame is null && FindDescendant<Frame>(root!) is { } f)
						{
							frame = f;
							if (!f.IsLoaded)
							{
								f.Loaded += OnFrameLoaded;
							}
						}

						return frame is not null;
					},
					cts.Token);
			}

			void OnFrameLoaded(object s, RoutedEventArgs e)
			{
				frame!.Loaded -= OnFrameLoaded;

				// Re-graft equivalent: the host detaches the app content mid-bootstrap
				// (production: Hot Design moves Window content into its own host).
				// Runs inside the Loaded dispatch — before the queued child-forwarding
				// continuation of the in-flight initial navigation.
				detachedDuringLoad = true;
				window.Content = new Border();
			}

			// The initial navigation completes (today: "successfully", having silently
			// dropped the child-bound route because the frame region detached).
			using (var initCts = new CancellationTokenSource(TestTimeout))
			{
				var completed = await Task.WhenAny(initTask, Task.Delay(Timeout.Infinite, initCts.Token));
				completed.Should().Be(initTask, "initial navigation should complete (not hang) while the content is detached");
			}

			host = await initTask;

			// Preconditions for the scenario: the detach really happened during load and
			// the frame is still empty (the route was dropped, not delivered).
			detachedDuringLoad.Should().BeTrue("the Frame.Loaded hook must observe the load (scenario precondition)");
			frame!.Content.Should().BeNull("the dropped child-bound route must not have reached the frame yet (scenario precondition)");

			// Re-graft completes: the app content returns to the tree (production: Hot
			// Design's host materializes its template and re-attaches the content).
			window.Content = root;

			// The initial route must reach the frame once its region re-attaches.
			using (var cts = new CancellationTokenSource(TestTimeout))
			{
				await UIHelper.WaitFor(
					() => frame!.Content is TestPageOne,
					cts.Token);
			}

			(frame!.Content is TestPageOne).Should().BeTrue(
				"the initial navigation must resume after the content is re-attached — " +
				"a dropped child-forward that never resumes leaves the shell permanently blank (spec 006)");
		}
		finally
		{
			if (host is not null)
			{
				await host.StopAsync();
			}
		}
	}

	private static T? FindDescendant<T>(DependencyObject parent)
		where T : DependencyObject
	{
		var count = VisualTreeHelper.GetChildrenCount(parent);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is T match)
			{
				return match;
			}

			if (FindDescendant<T>(child) is { } nested)
			{
				return nested;
			}
		}

		return default;
	}
}
