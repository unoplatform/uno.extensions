using System;
using System.Linq;
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
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Navigating to a page that hosts no nested regions consumes the whole route, and the
/// navigator then forwards the empty remainder to a region with no children. That is
/// the normal case for most pages and must not log at Warning; a non-empty remainder
/// with nowhere to go is a real misconfiguration and must still warn.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_LeafPageNavigation
{
	private const string NoChildrenWarning = "has no children to forward request to";

	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

	[TestMethod]
	public async Task When_NavigatingToPageWithoutRegions_Then_NoChildrenWarningIsNotLogged()
	{
		using var cts = new CancellationTokenSource(Timeout);
		var logs = new CapturingLoggerProvider();

		await RunAppAsync(logs, async frameNav =>
		{
			await frameNav.NavigateRouteAsync(this, "TestPageTwo");
			await UIHelper.WaitFor(() => frameNav.Route?.Base == "TestPageTwo", cts.Token);
		}, cts.Token);

		logs.Entries
			.Where(e => e.Level >= LogLevel.Warning && e.Message.Contains(NoChildrenWarning))
			.Select(e => e.Message)
			.Should().BeEmpty("a page with no nested regions has nothing left to forward once the route is consumed");
	}

	[TestMethod]
	public async Task When_RouteHasUnconsumedSegment_And_PageHasNoRegions_Then_NoChildrenWarningIsLogged()
	{
		using var cts = new CancellationTokenSource(Timeout);
		var logs = new CapturingLoggerProvider();

		await RunAppAsync(logs, async frameNav =>
		{
			await frameNav.NavigateRouteAsync(this, "TestPageTwo/Missing");
		}, cts.Token);

		logs.Entries
			.Should().Contain(
				e => e.Level == LogLevel.Warning && e.Message.Contains(NoChildrenWarning) && e.Message.Contains("Missing"),
				"a leftover route segment with no child region to receive it is a misconfiguration");
	}

	/// <summary>
	/// Boots a navigation host in the runtime-tests window (a fresh <c>new Window()</c> is not
	/// portable — see AGENTS.md), waits for TestPageOne, then runs <paramref name="act"/> against
	/// the FrameView's inner navigator (a Page navigated into a ContentControl root is wrapped
	/// in a FrameView — see ContentControlNavigator.Show).
	/// </summary>
	private static async Task RunAppAsync(CapturingLoggerProvider logs, Func<INavigator, Task> act, CancellationToken ct)
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
					.CreateDefaultBuilder(typeof(Given_LeafPageNavigation).Assembly)
					.ConfigureServices(services => services.AddLogging(logging => logging
						.SetMinimumLevel(LogLevel.Trace)
						.AddProvider(logs)))
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
					.Build(),
				navigationRoot: navigationRoot,
				initialRoute: "TestPageOne");

			INavigator? frameNav = null;
			await UIHelper.WaitFor(() => (frameNav = (navigationRoot.Content as FrameView)?.Navigator) is not null, ct);
			await UIHelper.WaitFor(() => frameNav!.Route?.Base == "TestPageOne", ct);

			await act(frameNav!);
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
