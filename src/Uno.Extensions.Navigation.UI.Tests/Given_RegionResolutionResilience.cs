using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.UI;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Regression tests for the "blank screen" bug where a <see cref="NavigationRegion"/> created from
/// <c>uen:Region.Attached="True"</c> has its view <c>Loaded</c> fire while its visual ancestry up to the
/// navigation root is transiently not connected (async tree construction / hot-reload view-swap). On the
/// pre-fix code the region resolved neither a parent region nor a root service provider, logged
/// <c>"Unable to find service provider for root navigator"</c>, and then permanently orphaned itself
/// (committed <c>_isLoaded</c> and unsubscribed its load events with no retry) — producing a blank screen.
///
/// The defect is build-independent logic; this test reproduces it deterministically by controlling the
/// order of events (region loads first, the navigation root becomes reachable second), independent of the
/// release-vs-debug timing that makes the race more or less likely in the wild.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_RegionResolutionResilience
{
	[TestMethod]
	public async Task When_RegionLoadsBeforeRootReachable_Then_RecoversWhenRootAppears()
	{
		// Arrange — a real navigation host (supplies INavigatorFactory, the scoped IServiceProvider, etc.).
		var host = UnoHost
			.CreateDefaultBuilder(typeof(Given_RegionResolutionResilience).Assembly)
			.UseNavigation(viewRouteBuilder: (views, routes) => { })
			.Build();

		// A ContentControl that will *become* the navigation root, but is NOT wired yet:
		// no Region.ServiceProvider attached, no root Region.Instance created.
		var root = new ContentControl
		{
			HorizontalAlignment = HorizontalAlignment.Stretch,
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch,
		};

		// A structural region (uen:Region.Attached="True") hosted inside `root`. Setting Attached
		// creates its NavigationRegion and subscribes the view's Loading/Loaded.
		var child = new Grid();
		child.SetAttached(true);
		root.Content = child;

		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		var childLoaded = new TaskCompletionSource();
		void OnChildLoaded(object sender, RoutedEventArgs e) => childLoaded.TrySetResult();
		child.Loaded += OnChildLoaded;

		UnitTestsUIContentHelper.SaveOriginalContent();
		try
		{
			// Act 1 — load the child while no navigation root is reachable. Its AssignParent finds
			// neither a parent region nor a service provider in its ancestry.
			window.Content = root;
			await childLoaded.Task.WaitAsync(TimeSpan.FromSeconds(30));

			// Precondition: the region exists but is unresolved (no navigator yet) — true both pre- and
			// post-fix at this point; documents the state the recovery must heal.
			child.FindRegion().Should().NotBeNull();
			child.FindRegion()!.Navigator().Should().BeNull("the region cannot resolve a navigator before a root exists");

			// Act 2 — make the navigation root reachable: attach the service provider and create the
			// root region on `root` (mirrors FrameworkElementExtensions.HostAsync / BuildAndInitializeHostAsync).
			var rootServices = await window.AttachServicesAsync(host.Services);
			_ = new NavigationRegion(
				host.Services.GetRequiredService<ILogger<NavigationRegion>>(),
				root,
				rootServices.CreateNavigationScope());

			// A layout pass accompanies a real (hot-reload) re-graft and is the signal the fix watches.
			// Force a *real* pass (invalidate first so UpdateLayout is not a no-op) so the test does not
			// depend on incidental layout activity.
			child.InvalidateMeasure();
			child.InvalidateArrange();
			root.UpdateLayout();

			// Assert — the previously-unresolved region recovers: it attaches to the root region and
			// resolves a navigator. Pre-fix this never happens (orphaned permanently) so WaitFor times out.
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			await UIHelper.WaitFor(() => child.FindRegion()?.Navigator() is not null, cts.Token);

			child.FindRegion()!.Parent.Should()
				.NotBeNull("the recovered region must be attached to the root region");
			child.FindRegion()!.Navigator().Should()
				.NotBeNull("an orphaned region must recover once the navigation root becomes reachable");
		}
		finally
		{
			child.Loaded -= OnChildLoaded;
			UnitTestsUIContentHelper.RestoreOriginalContent();
			(host as IDisposable)?.Dispose();
		}
	}
}
