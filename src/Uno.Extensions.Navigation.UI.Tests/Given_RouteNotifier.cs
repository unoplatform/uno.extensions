using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation.Regions;
using Uno.Extensions.Navigation.UI.Tests.Pages;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_RouteNotifier
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	/// <summary>
	/// Helper to build a navigation host, initialize it on the given Window,
	/// and return the host + root navigator + route notifier.
	/// </summary>
	private async Task<(IHost Host, INavigator Navigator, IRouteNotifier Notifier, ContentControl Root)> SetupNavigationAsync()
	{
		var window = new Window();

		IHost? host = null;
		host = await window.InitializeNavigationAsync(
			buildHost: async () =>
			{
				var h = UnoHost
					.CreateDefaultBuilder(typeof(Given_RouteNotifier).Assembly)
					.UseNavigation(
						viewRouteBuilder: (views, routes) =>
						{
							views.Register(
								new ViewMap<TestPageOne>(),
								new ViewMap<TestPageTwo>(),
								new ViewMap<TestPageThree>());

							routes.Register(
								new RouteMap("", Nested: new RouteMap[]
								{
									new RouteMap("TestPageOne", View: views.FindByView<TestPageOne>()),
									new RouteMap("TestPageTwo", View: views.FindByView<TestPageTwo>()),
									new RouteMap("TestPageThree", View: views.FindByView<TestPageThree>()),
								}));
						})
					.Build();
				return h;
			},
			initialRoute: "TestPageOne");

		var notifier = host!.Services.GetRequiredService<IRouteNotifier>();
		var root = (ContentControl)window.Content!;
		var navigator = root.Navigator()!;

		return (host, navigator, notifier, root);
	}

	[TestMethod]
	public async Task When_InitialNavigation_Then_RouteChanged_Has_Route()
	{
		// Arrange
		var eventReceived = new TaskCompletionSource<RouteChangedEventArgs>();

		var window = new Window();

		IRouteNotifier? notifier = null;
		var host = await window.InitializeNavigationAsync(
			buildHost: async () =>
			{
				var h = UnoHost
					.CreateDefaultBuilder(typeof(Given_RouteNotifier).Assembly)
					.UseNavigation(
						viewRouteBuilder: (views, routes) =>
						{
							views.Register(
								new ViewMap<TestPageOne>(),
								new ViewMap<TestPageTwo>());

							routes.Register(
								new RouteMap("", Nested: new RouteMap[]
								{
									new RouteMap("TestPageOne", View: views.FindByView<TestPageOne>()),
									new RouteMap("TestPageTwo", View: views.FindByView<TestPageTwo>()),
								}));
						})
					.Build();

				notifier = h.Services.GetRequiredService<IRouteNotifier>();
				notifier.RouteChanged += (s, e) =>
				{
					eventReceived.TrySetResult(e);
				};

				return h;
			},
			initialRoute: "TestPageOne");

		// Act - wait for initial navigation to complete
		using var cts = new CancellationTokenSource(Timeout);
		cts.Token.Register(() => eventReceived.TrySetCanceled());
		var args = await eventReceived.Task;

		// Assert
		args.Should().NotBeNull("RouteChanged should have been raised");
		args.Route.Should().NotBeNull("Route property should be populated");
		args.Route!.ToString().Should().Contain("TestPageOne", "Route should contain the navigated page name");
		args.Region.Should().NotBeNull("Region should be populated");

		// Cleanup
		await host.StopAsync();
	}

	[TestMethod]
	public async Task When_NavigateForward_Then_RouteChanged_Has_Route()
	{
		// Arrange
		var (host, navigator, notifier, root) = await SetupNavigationAsync();

		RouteChangedEventArgs? receivedArgs = null;
		var eventReceived = new TaskCompletionSource<RouteChangedEventArgs>();
		notifier.RouteChanged += (s, e) =>
		{
			receivedArgs = e;
			eventReceived.TrySetResult(e);
		};

		// Act - navigate to TestPageTwo
		await navigator.NavigateRouteAsync(root, "TestPageTwo");

		using var cts = new CancellationTokenSource(Timeout);
		cts.Token.Register(() => eventReceived.TrySetCanceled());
		var args = await eventReceived.Task;

		// Assert
		args.Should().NotBeNull("RouteChanged should have been raised on forward navigation");
		args.Route.Should().NotBeNull("Route property should be populated on forward navigation");
		args.Route!.ToString().Should().Contain("TestPageTwo", "Route should reflect the new page");
		args.Navigator.Should().NotBeNull("Navigator should be populated");

		// Cleanup
		await host.StopAsync();
	}

	[TestMethod]
	public async Task When_NavigateBack_Then_RouteChanged_Has_Route()
	{
		// Arrange
		var (host, navigator, notifier, root) = await SetupNavigationAsync();

		// Navigate to TestPageTwo first
		await navigator.NavigateRouteAsync(root, "TestPageTwo");

		// Now set up listener for back navigation
		var backEventReceived = new TaskCompletionSource<RouteChangedEventArgs>();
		notifier.RouteChanged += (s, e) =>
		{
			backEventReceived.TrySetResult(e);
		};

		// Act - navigate back
		await navigator.NavigateBackAsync(root);

		using var cts = new CancellationTokenSource(Timeout);
		cts.Token.Register(() => backEventReceived.TrySetCanceled());
		var args = await backEventReceived.Task;

		// Assert
		args.Should().NotBeNull("RouteChanged should have been raised on back navigation");
		args.Route.Should().NotBeNull("Route property should be populated on back navigation");
		args.Route!.ToString().Should().Contain("TestPageOne", "Route should reflect the page we navigated back to");

		// Cleanup
		await host.StopAsync();
	}

	[TestMethod]
	public async Task When_MultipleNavigations_Then_Each_RouteChanged_Has_Route()
	{
		// Arrange
		var (host, navigator, notifier, root) = await SetupNavigationAsync();

		var routeChangedArgs = new System.Collections.Generic.List<RouteChangedEventArgs>();
		notifier.RouteChanged += (s, e) =>
		{
			routeChangedArgs.Add(e);
		};

		// Act - navigate through multiple pages
		await navigator.NavigateRouteAsync(root, "TestPageTwo");

		await navigator.NavigateRouteAsync(root, "TestPageThree");

		// Assert - each navigation should have produced a RouteChanged with a populated Route
		using var waitCts = new CancellationTokenSource(Timeout);
		await UIHelper.WaitFor(() => routeChangedArgs.Count >= 2, waitCts.Token);

		routeChangedArgs.Should().HaveCountGreaterOrEqualTo(2, "Each navigation should fire RouteChanged");

		foreach (var args in routeChangedArgs)
		{
			args.Route.Should().NotBeNull("Every RouteChanged event should have a populated Route");
			args.Region.Should().NotBeNull("Every RouteChanged event should have a populated Region");
		}

		// Verify the routes contain expected page names
		routeChangedArgs.Should().Contain(
			a => a.Route!.ToString().Contains("TestPageTwo"),
			"One of the RouteChanged events should contain TestPageTwo");
		routeChangedArgs.Should().Contain(
			a => a.Route!.ToString().Contains("TestPageThree"),
			"One of the RouteChanged events should contain TestPageThree");

		// Cleanup
		await host.StopAsync();
	}

	[TestMethod]
	public async Task When_RouteChanged_Then_Route_Matches_Region_GetRoute()
	{
		// Arrange
		var (host, navigator, notifier, root) = await SetupNavigationAsync();

		var eventReceived = new TaskCompletionSource<RouteChangedEventArgs>();
		notifier.RouteChanged += (s, e) =>
		{
			eventReceived.TrySetResult(e);
		};

		// Act - navigate to TestPageTwo
		await navigator.NavigateRouteAsync(root, "TestPageTwo");

		using var cts = new CancellationTokenSource(Timeout);
		cts.Token.Register(() => eventReceived.TrySetCanceled());
		var args = await eventReceived.Task;

		// Assert - Route in event args should match what GetRoute() returns
		var regionRoute = args.Region.Root().GetRoute();

		args.Route.Should().NotBeNull("Route should be populated in event args");
		regionRoute.Should().NotBeNull("Region.GetRoute() should also return a route");

		// The Route in the event args should be the same as what Region.GetRoute() returns
		args.Route!.ToString().Should().Be(
			regionRoute!.ToString(),
			"Route in event args should match the route reconstructed from the region tree");

		// Cleanup
		await host.StopAsync();
	}

	[TestMethod]
	public async Task When_RouteChanged_Then_Navigator_Is_Not_Null()
	{
		// Arrange
		var (host, navigator, notifier, root) = await SetupNavigationAsync();

		var eventReceived = new TaskCompletionSource<RouteChangedEventArgs>();
		notifier.RouteChanged += (s, e) =>
		{
			eventReceived.TrySetResult(e);
		};

		// Act
		await navigator.NavigateRouteAsync(root, "TestPageTwo");

		using var cts = new CancellationTokenSource(Timeout);
		cts.Token.Register(() => eventReceived.TrySetCanceled());
		var args = await eventReceived.Task;

		// Assert
		args.Navigator.Should().NotBeNull("Navigator should be provided in RouteChanged event args");

		// Cleanup
		await host.StopAsync();
	}

	[TestMethod]
	public async Task When_RouteChanged_Then_Route_Has_NonEmpty_Base()
	{
		// Arrange
		var (host, navigator, notifier, root) = await SetupNavigationAsync();

		var eventReceived = new TaskCompletionSource<RouteChangedEventArgs>();
		notifier.RouteChanged += (s, e) =>
		{
			eventReceived.TrySetResult(e);
		};

		// Act
		await navigator.NavigateRouteAsync(root, "TestPageTwo");

		using var cts = new CancellationTokenSource(Timeout);
		cts.Token.Register(() => eventReceived.TrySetCanceled());
		var args = await eventReceived.Task;

		// Assert - The Route should have a non-empty Base property
		args.Route.Should().NotBeNull();
		args.Route!.Base.Should().NotBeNullOrWhiteSpace("Route.Base should identify the navigated page");

		// Cleanup
		await host.StopAsync();
	}
}
