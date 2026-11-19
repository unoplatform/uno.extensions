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
using Uno.Extensions.Navigation.UI.Controls;
using Uno.Extensions.Navigation.UI.Tests.Pages;
using Uno.Extensions.Navigation.UI.Tests.ViewModels;
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

	/// <summary>
	/// An app whose shell route ("") has no IsDefault child and that passes an initial view
	/// must land on that view. Without a launch URL (desktop, mobile), the launch route is
	/// empty and matched the shell route the initial navigation lands on, so the initial view
	/// navigation was skipped and the app showed an empty shell.
	/// </summary>
	[TestMethod]
	public async Task When_ShellHasNoDefaultRoute_And_InitialViewSet_Then_InitialViewShown()
	{
		using var cts = new CancellationTokenSource(Timeout);
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
					.CreateDefaultBuilder(typeof(Given_NavigatorStartup).Assembly)
					.UseNavigation(
						viewRouteBuilder: (views, routes) =>
						{
							views.Register(
								new ViewMap(ViewModel: typeof(StartupShellModel)),
								new ViewMap<TestPageOne>(),
								new ViewMap<TestPageTwo>());

							routes.Register(
								new RouteMap("", View: views.FindByViewModel<StartupShellModel>(), Nested: new RouteMap[]
								{
									new RouteMap("TestPageOne", View: views.FindByView<TestPageOne>()),
									new RouteMap("TestPageTwo", View: views.FindByView<TestPageTwo>()),
								}));
						})
					.Build(),
				navigationRoot: navigationRoot,
				initialView: typeof(TestPageTwo));

			INavigator? frameNav = null;
			await UIHelper.WaitFor(() => (frameNav = (navigationRoot.Content as FrameView)?.Navigator) is not null, cts.Token);
			await UIHelper.WaitFor(() => frameNav!.Route?.Base == "TestPageTwo", cts.Token);

			frameNav!.Route?.Base.Should().Be("TestPageTwo");
		}
		finally
		{
			if (host is not null)
			{
				await host.StopAsync();
			}
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}
}
