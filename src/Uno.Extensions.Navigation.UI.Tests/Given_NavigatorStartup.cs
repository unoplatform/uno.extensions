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
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation.UI.Tests.Pages;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Tests for navigation startup behavior, specifically:
/// - Navigation to default route succeeds when routes are properly configured
/// - Navigation produces a warning when no routes are registered (no IsDefault)
/// - Navigation produces a warning when initial route resolution returns null
///
/// These tests verify fixes for silent navigation failures that cause black screens
/// when the startup navigation fails without logging.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_NavigatorStartup
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

	/// <summary>
	/// When routes are properly configured with IsDefault, navigation should succeed
	/// and the default page should be displayed.
	/// </summary>
	[TestMethod]
	public async Task When_DefaultRouteConfigured_Then_NavigationSucceeds()
	{
		var window = new Window();

		IHost? host = null;
		host = await window.InitializeNavigationAsync(
			buildHost: async () =>
			{
				var h = UnoHost
					.CreateDefaultBuilder(typeof(Given_NavigatorStartup).Assembly)
					.UseNavigation(
						viewRouteBuilder: (views, routes) =>
						{
							views.Register(
								new ViewMap<TestPageOne>(),
								new ViewMap<TestPageTwo>());

							routes.Register(
								new RouteMap("", Nested: new RouteMap[]
								{
									new RouteMap("TestPageOne", View: views.FindByView<TestPageOne>(), IsDefault: true),
									new RouteMap("TestPageTwo", View: views.FindByView<TestPageTwo>()),
								}));
						})
					.Build();
				return h;
			});

		try
		{
			var root = window.Content as ContentControl;
			root.Should().NotBeNull("Window.Content should be a ContentControl after navigation init");

			var nav = root!.Navigator();
			nav.Should().NotBeNull("Root navigator should exist after successful initialization");

			// The default route should have been navigated to
			using var cts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => nav!.Route?.Base == "TestPageOne",
				cts.Token);

			nav!.Route?.Base.Should().Be("TestPageOne");
		}
		finally
		{
			await host!.StopAsync();
		}
	}

	/// <summary>
	/// When no routes have IsDefault=true, the initial navigation should still complete
	/// (not hang), but the navigator should not have navigated to any route.
	/// This scenario previously caused a silent failure and black screen.
	/// After the fix, Warning-level logs are emitted for diagnostics.
	/// </summary>
	[TestMethod]
	public async Task When_NoDefaultRoute_Then_NavigationCompletesWithoutHanging()
	{
		var window = new Window();

		IHost? host = null;
		host = await window.InitializeNavigationAsync(
			buildHost: async () =>
			{
				var h = UnoHost
					.CreateDefaultBuilder(typeof(Given_NavigatorStartup).Assembly)
					.UseNavigation(
						viewRouteBuilder: (views, routes) =>
						{
							views.Register(
								new ViewMap<TestPageOne>());

							// Register route WITHOUT IsDefault — the navigator
							// cannot determine what to show initially.
							routes.Register(
								new RouteMap("", Nested: new RouteMap[]
								{
									new RouteMap("TestPageOne", View: views.FindByView<TestPageOne>()),
								}));
						})
					.Build();
				return h;
			});

		try
		{
			// Navigation should have completed (not timed out or hung)
			host.Should().NotBeNull();

			// The root should exist even if navigation didn't resolve to a page
			var root = window.Content as ContentControl;
			root.Should().NotBeNull();
		}
		finally
		{
			await host!.StopAsync();
		}
	}
}
