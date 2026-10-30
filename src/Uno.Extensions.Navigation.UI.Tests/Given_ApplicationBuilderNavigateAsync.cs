using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
using Uno.Extensions.Navigation.UI.Tests.Pages.ChainedResult;
using Uno.Extensions.Navigation.UI.Tests.ViewModels;
using Uno.UI.RuntimeTests;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// <see cref="ApplicationBuilderExtensions.NavigateAsync{TShell}"/> needs a <see cref="ContentControl"/>
/// inside the shell to host navigation. Since Uno 7, <see cref="Page"/> and <see cref="UserControl"/> derive
/// from <see cref="Control"/>, so a Page shell used to leave navigation on a detached root: the initial
/// request parked forever behind a blank screen (issue #3225).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_ApplicationBuilderNavigateAsync
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

	[TestMethod]
	public async Task When_ShellIsPage_Then_Throws()
	{
		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		UnitTestsUIContentHelper.SaveOriginalContent();
		var originalContent = window.Content;
		try
		{
			var act = () => new FakeApplicationBuilder(window).NavigateAsync<TestPageOne>().WaitAsync(Timeout);

			(await act.Should().ThrowAsync<InvalidOperationException>())
				.WithMessage($"*{typeof(TestPageOne).FullName}*{nameof(IContentControlProvider)}*");
			window.Content.Should().BeSameAs(originalContent, "a rejected shell must leave the window untouched");
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}

	[TestMethod]
	public async Task When_ShellIsContentControl_Then_HostsNavigation()
	{
		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		UnitTestsUIContentHelper.SaveOriginalContent();
		var logs = new CapturingLoggerProvider();
		IHost? host = null;
		try
		{
			// Explicit initial route: descending from "" to an IsDefault leaf is not what's under test (see HotReload.Spec.md).
			host = await new FakeApplicationBuilder(window, logs: logs)
				.NavigateAsync<ShellPage>(initialNavigate: (_, nav) => nav.NavigateRouteAsync(nav, "TestPageOne"))
				.WaitAsync(Timeout);

			var shell = window.Content.Should().BeOfType<ShellPage>().Subject;
			using var cts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(() => (shell.Content as FrameView)?.Navigator?.Route?.Base == "TestPageOne", cts.Token);
			logs.Entries.Should().NotContain(e => e.Message.Contains(typeof(ShellPage).FullName!), "the shell is not a route target");
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

	/// <summary>
	/// A shell that is its own provider and also the view of the default route is navigated into itself.
	/// That still works, so it isn't an error, but it's never intended.
	/// </summary>
	[TestMethod]
	public async Task When_ShellIsAlsoInitialRouteView_Then_LogsNestingWarning()
	{
		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		UnitTestsUIContentHelper.SaveOriginalContent();
		var logs = new CapturingLoggerProvider();
		IHost? host = null;
		try
		{
			host = await new FakeApplicationBuilder(
					window,
					(views, routes) =>
					{
						views.Register(new ViewMap<ProviderShellPage, ProviderShellModel>());
						routes.Register(new RouteMap("Main", View: views.FindByViewModel<ProviderShellModel>(), IsDefault: true));
					},
					logs)
				.NavigateAsync<ProviderShellPage>()
				.WaitAsync(Timeout);

			var shell = window.Content.Should().BeOfType<ProviderShellPage>().Subject;
			// The route's view is the shell type, so a second instance is created. The FrameView's Frame
			// only navigates to it after startup completes.
			using var cts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(() => shell.ContentControl.Content is FrameView { Content: Frame { Content: ProviderShellPage } }, cts.Token);
			logs.Entries.Should().Contain(
				e => e.Level == LogLevel.Warning && e.Message.Contains(typeof(ProviderShellPage).FullName!) && e.Message.Contains("nested"));
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

	private static void RegisterTestPageRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(new ViewMap<TestPageOne>());
		routes.Register(
			new RouteMap("", Nested: new RouteMap[]
			{
				new RouteMap("TestPageOne", View: views.FindByView<TestPageOne>()),
			}));
	}

	/// <summary>
	/// The real builder is created from <c>OnLaunched</c> arguments; navigation only needs the window and the host.
	/// </summary>
	private sealed class FakeApplicationBuilder(
		Window window,
		Action<IViewRegistry, IRouteRegistry>? viewRouteBuilder = null,
		CapturingLoggerProvider? logs = null) : IApplicationBuilder
	{
		public Application App => Application.Current;

		public LaunchActivatedEventArgs Arguments => throw new NotSupportedException();

		public Window Window => window;

		public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

		public IApplicationBuilder Configure(Action<IHostBuilder> configureHost) => throw new NotSupportedException();

		public IApplicationBuilder Configure(Action<IHostBuilder, Window> configureHost) => throw new NotSupportedException();

		[RequiresDynamicCode("Binds configuration.")]
		[RequiresUnreferencedCode("Binds configuration.")]
		public IHost Build() => UnoHost
			.CreateDefaultBuilder(typeof(Given_ApplicationBuilderNavigateAsync).Assembly)
			.ConfigureServices(services =>
			{
				if (logs is not null)
				{
					services.AddLogging(logging => logging.AddProvider(logs));
				}
			})
			.UseNavigation(viewRouteBuilder: viewRouteBuilder ?? RegisterTestPageRoutes)
			.Build();
	}
}
