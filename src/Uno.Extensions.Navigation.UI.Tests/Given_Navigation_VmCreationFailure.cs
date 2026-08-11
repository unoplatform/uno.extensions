using System;
using System.Collections.Concurrent;
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
/// Tests for how navigation surfaces a view model whose constructor (or DI
/// dependency chain) throws (#3136). The failure must:
/// - fault the navigation task with the original exception (not hang),
/// - be logged at Error level (previously nothing was logged at any level),
/// - leave the navigator usable for subsequent navigations
///   (RouteUpdater.EndNavigation must run even when navigation faults).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_Navigation_VmCreationFailure
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

	public sealed class ThrowingCtorViewModel
	{
		public const string FailureMessage = "ThrowingCtorViewModel: constructor failure";

		public ThrowingCtorViewModel()
		{
			throw new InvalidOperationException(FailureMessage);
		}
	}

	private sealed class CapturingLoggerProvider : ILoggerProvider
	{
		public ConcurrentQueue<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

		public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

		public void Dispose()
		{
		}

		private sealed class CapturingLogger : ILogger
		{
			private readonly CapturingLoggerProvider _owner;

			public CapturingLogger(CapturingLoggerProvider owner)
			{
				_owner = owner;
			}

			public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default;

			public bool IsEnabled(LogLevel logLevel) => true;

			public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
				=> _owner.Entries.Enqueue((logLevel, formatter(state, exception), exception));
		}
	}

	private sealed class TestApp : IAsyncDisposable
	{
		private readonly IHost _host;

		public TestApp(ContentControl navigationRoot, INavigator frameNavigator, IHost host, CapturingLoggerProvider logs)
		{
			NavigationRoot = navigationRoot;
			FrameNavigator = frameNavigator;
			Logs = logs;
			_host = host;
		}

		public ContentControl NavigationRoot { get; }

		public INavigator FrameNavigator { get; }

		public CapturingLoggerProvider Logs { get; }

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

	/// <summary>
	/// Boots an Uno host with navigation, hosted in the runtime-tests engine's
	/// already-displayed test window (a fresh <c>new Window()</c> never fires
	/// Loaded/Activate in this harness — see Given_HotReload.SetupAppAsync), and
	/// navigates to TestPageOne. Navigation targets the FrameView's inner
	/// navigator because a Page navigated into a ContentControl root is wrapped
	/// in a FrameView (see ContentControlNavigator.Show).
	/// </summary>
	private static async Task<TestApp> SetupAppAsync(CancellationToken ct)
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

		var logs = new CapturingLoggerProvider();
		IHost? host = null;
		try
		{
			host = await window.InitializeNavigationAsync(
				buildHost: async () => UnoHost
					.CreateDefaultBuilder(typeof(Given_Navigation_VmCreationFailure).Assembly)
					.ConfigureServices(services => services.AddLogging(logging => logging.AddProvider(logs)))
					.UseNavigation(
						viewRouteBuilder: (views, routes) =>
						{
							views.Register(
								new ViewMap<TestPageOne>(),
								new ViewMap<TestPageTwo, ThrowingCtorViewModel>(),
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

			var frameNav = await WaitForFrameNavigatorAsync(navigationRoot, Timeout, ct);
			await WaitForRouteAsync(frameNav, "TestPageOne", Timeout, ct);

			return new TestApp(navigationRoot, frameNav, host, logs);
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

	[TestMethod]
	public async Task When_VmCtorThrows_Then_NavigationFaults_And_ErrorIsLogged()
	{
		using var cts = new CancellationTokenSource(Timeout);
		await using var app = await SetupAppAsync(cts.Token);

		var navigation = app.FrameNavigator.NavigateRouteAsync(this, "TestPageTwo");

		// The navigation must complete (faulted), not hang — the hang is the
		// original #3136 symptom.
		var completed = await Task.WhenAny(navigation, Task.Delay(Timeout));
		completed.Should().Be(navigation, "a failing view-model constructor must fault the navigation, not hang it");

		var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => navigation);
		exception.Message.Should().Be(ThrowingCtorViewModel.FailureMessage, "the original constructor exception must propagate unwrapped");

		var errors = app.Logs.Entries.Where(e => e.Level == LogLevel.Error).ToArray();
		errors.Should().Contain(
			e => e.Exception is InvalidOperationException && e.Message.Contains(nameof(ThrowingCtorViewModel)),
			"the view-model construction failure must be logged at Error with the view-model type");
	}

	[TestMethod]
	public async Task When_VmCtorThrows_Then_SubsequentNavigationStillWorks()
	{
		using var cts = new CancellationTokenSource(Timeout);
		await using var app = await SetupAppAsync(cts.Token);

		var navigation = app.FrameNavigator.NavigateRouteAsync(this, "TestPageTwo");
		await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => navigation);

		// The faulted navigation must not wedge the pipeline: EndNavigation ran,
		// so a follow-up navigation on the same navigator succeeds.
		await app.FrameNavigator.NavigateRouteAsync(this, "TestPageThree");

		using var routeCts = new CancellationTokenSource(Timeout);
		await WaitForRouteAsync(app.FrameNavigator, "TestPageThree", Timeout, routeCts.Token);

		app.FrameNavigator.Route?.Base.Should().Be("TestPageThree");
	}

	private static async Task<INavigator> WaitForFrameNavigatorAsync(ContentControl root, TimeSpan timeout, CancellationToken ct)
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

	private static async Task WaitForRouteAsync(INavigator nav, string expectedBase, TimeSpan timeout, CancellationToken ct)
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
			$"Last state: Route='{nav.Route?.Base ?? "<null>"}'.");
	}
}
