using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation.UI.Controls;
using Uno.Extensions.Navigation.UI.Tests.Pages.Startup;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Startup navigation matrix: for each way an app can start (initial route, initialNavigate, initialView,
/// initialViewModel, launch route), how many times each page and view model is constructed, which page
/// ends up on screen, and what is left in the back stack.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_StartupNavigation
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

	[TestMethod]
	public async Task When_NoInitialNavigate_Then_DefaultPageCreatedOnce()
	{
		var state = await StartAsync<StartupMainPage>(ShellRoutes);

		state.ShouldShow<StartupMainPage>(backStackDepth: 0);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupMainPage>();
		state.ShouldHaveCreatedOnce<StartupMainModel>();
	}

	[TestMethod]
	public async Task When_InitialRouteIsDefaultPage_Then_CreatedOnce()
	{
		var state = await StartAsync<StartupMainPage>(ShellRoutes, initialRoute: "Main");

		state.ShouldShow<StartupMainPage>(backStackDepth: 0);
		state.Count<StartupShellModel>().Should().Be(0, "an explicit initial route starts below the empty shell route ({0})", state.Created);
		state.ShouldHaveCreatedOnce<StartupMainPage>();
		state.ShouldHaveCreatedOnce<StartupMainModel>();
	}

	[TestMethod]
	public async Task When_InitialNavigateToDefaultPage_Then_CreatedOnce()
	{
		var state = await StartAsync<StartupMainPage>(
			ShellRoutes,
			initialNavigate: (_, nav) => nav.NavigateViewModelAsync<StartupMainModel>(this));

		state.ShouldShow<StartupMainPage>(backStackDepth: 0);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupMainPage>();
		state.ShouldHaveCreatedOnce<StartupMainModel>();
	}

	/// <summary>
	/// The recommended template with authentication, for a user who is already signed in.
	/// </summary>
	[TestMethod]
	public async Task When_InitialNavigateToDefaultPageClearingBackStack_Then_CreatedOnce()
	{
		var state = await StartAsync<StartupMainPage>(
			ShellRoutes,
			initialNavigate: (_, nav) => nav.NavigateViewModelAsync<StartupMainModel>(this, qualifier: Qualifiers.ClearBackStack));

		state.ShouldShow<StartupMainPage>(backStackDepth: 0);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupMainPage>();
		// A ClearBackStack request for the page that's already shown gives it a fresh view model
		// (FrameNavigator); the page itself isn't rebuilt.
		state.Count<StartupMainModel>().Should().BeInRange(1, 2, "only the view model may be refreshed ({0})", state.Created);
	}

	/// <summary>
	/// The recommended template with authentication, for a user who isn't signed in.
	/// </summary>
	[TestMethod]
	public async Task When_InitialNavigateToOtherPageClearingBackStack_Then_OtherPageShownAlone()
	{
		var state = await StartAsync<StartupLoginPage>(
			ShellRoutes,
			initialNavigate: (_, nav) => nav.NavigateViewModelAsync<StartupLoginModel>(this, qualifier: Qualifiers.ClearBackStack));

		state.ShouldShow<StartupLoginPage>(backStackDepth: 0);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupLoginPage>();
		state.ShouldHaveCreatedOnce<StartupLoginModel>();
		state.ShouldHaveCreatedAtMostOnce<StartupMainPage>();
		state.ShouldHaveCreatedAtMostOnce<StartupMainModel>();
	}

	[TestMethod]
	public async Task When_InitialNavigateDoesNotNavigate_Then_DefaultPageCreatedOnce()
	{
		var state = await StartAsync<StartupMainPage>(
			ShellRoutes,
			initialNavigate: (_, _) => Task.CompletedTask);

		state.ShouldShow<StartupMainPage>(backStackDepth: 0);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupMainPage>();
		state.ShouldHaveCreatedOnce<StartupMainModel>();
	}

	[TestMethod]
	public async Task When_InitialViewIsDefaultPage_Then_CreatedOnce()
	{
		var state = await StartAsync<StartupMainPage>(ShellRoutes, initialView: typeof(StartupMainPage));

		state.ShouldShow<StartupMainPage>(backStackDepth: 0);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupMainPage>();
		state.ShouldHaveCreatedOnce<StartupMainModel>();
	}

	[TestMethod]
	public async Task When_InitialViewModelIsDefaultPage_Then_CreatedOnce()
	{
		var state = await StartAsync<StartupMainPage>(ShellRoutes, initialViewModel: typeof(StartupMainModel));

		state.ShouldShow<StartupMainPage>(backStackDepth: 0);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupMainPage>();
		state.ShouldHaveCreatedOnce<StartupMainModel>();
	}

	[TestMethod]
	public async Task When_InitialViewModelIsOtherPage_Then_OtherPageShown()
	{
		var state = await StartAsync<StartupLoginPage>(ShellRoutes, initialViewModel: typeof(StartupLoginModel));

		state.ShouldShow<StartupLoginPage>(backStackDepth: null);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupLoginPage>();
		state.ShouldHaveCreatedOnce<StartupLoginModel>();
		state.ShouldHaveCreatedAtMostOnce<StartupMainPage>();
		state.ShouldHaveCreatedAtMostOnce<StartupMainModel>();
	}

	[TestMethod]
	public async Task When_NestedDefaults_Then_EachPageCreatedOnce()
	{
		var state = await StartAsync<StartupChildPage>(NestedRoutes);

		state.ShouldShow<StartupChildPage>(backStackDepth: 0);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupHostPage>();
		state.ShouldHaveCreatedOnce<StartupHostModel>();
		state.ShouldHaveCreatedOnce<StartupChildPage>();
		state.ShouldHaveCreatedOnce<StartupChildModel>();
	}

	/// <summary>
	/// A wizard-style app: the host page is the default route and has its own default child, and
	/// initialNavigate also navigates to the host.
	/// </summary>
	[TestMethod]
	public async Task When_InitialNavigateToNestedDefaultHost_Then_EachPageCreatedOnce()
	{
		var state = await StartAsync<StartupChildPage>(
			NestedRoutes,
			initialNavigate: (_, nav) => nav.NavigateViewModelAsync<StartupHostModel>(this));

		state.ShouldShow<StartupChildPage>(backStackDepth: 0);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupHostPage>();
		state.ShouldHaveCreatedOnce<StartupHostModel>();
		state.ShouldHaveCreatedOnce<StartupChildPage>();
		state.ShouldHaveCreatedOnce<StartupChildModel>();
	}

	/// <summary>
	/// A deep link to the page the default route already shows navigates once (#2607, fixed by #2639).
	/// </summary>
	[TestMethod]
	public async Task When_LaunchRouteIsDefaultPage_Then_CreatedOnce()
	{
		var state = await StartAsync<StartupMainPage>(
			ShellRoutes,
			launchUrl: "Main",
			initialNavigate: (_, nav) => nav.NavigateViewModelAsync<StartupLoginModel>(this, qualifier: Qualifiers.ClearBackStack));

		state.ShouldShow<StartupMainPage>(backStackDepth: 0);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupMainPage>();
		state.ShouldHaveCreatedOnce<StartupMainModel>();
		state.Count<StartupLoginPage>().Should().Be(0, "a launch route takes precedence over initialNavigate ({0})", state.Created);
	}

	[TestMethod]
	public async Task When_LaunchRouteIsOtherPage_Then_OtherPageShown()
	{
		var state = await StartAsync<StartupLoginPage>(ShellRoutes, launchUrl: "Login");

		state.ShouldShow<StartupLoginPage>(backStackDepth: null);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupLoginPage>();
		state.ShouldHaveCreatedOnce<StartupLoginModel>();
		state.ShouldHaveCreatedAtMostOnce<StartupMainPage>();
		state.ShouldHaveCreatedAtMostOnce<StartupMainModel>();
	}

	/// <summary>
	/// #2763: with no IsDefault route, the page initialNavigate goes to must still be shown.
	/// </summary>
	[TestMethod]
	public async Task When_NoDefaultRouteAndInitialNavigate_Then_TargetPageShown()
	{
		var state = await StartAsync<StartupMainPage>(
			ShellRoutesWithoutDefault,
			initialNavigate: (_, nav) => nav.NavigateViewModelAsync<StartupMainModel>(this));

		state.ShouldShow<StartupMainPage>(backStackDepth: 0);
		state.ShouldHaveCreatedOnce<StartupShellModel>();
		state.ShouldHaveCreatedOnce<StartupMainPage>();
		state.ShouldHaveCreatedOnce<StartupMainModel>();
	}

	private static void ShellRoutes(IViewRegistry views, IRouteRegistry routes)
		=> RegisterShellRoutes(views, routes, mainIsDefault: true);

	private static void ShellRoutesWithoutDefault(IViewRegistry views, IRouteRegistry routes)
		=> RegisterShellRoutes(views, routes, mainIsDefault: false);

	/// <summary>
	/// The recommended template's shape: a shell view model on "" (no view, so a FrameView is injected),
	/// with Main as the default page and Login next to it.
	/// </summary>
	private static void RegisterShellRoutes(IViewRegistry views, IRouteRegistry routes, bool mainIsDefault)
	{
		views.Register(
			new ViewMap(ViewModel: typeof(StartupShellModel)),
			new ViewMap<StartupMainPage, StartupMainModel>(),
			new ViewMap<StartupLoginPage, StartupLoginModel>());

		routes.Register(
			new RouteMap("", View: views.FindByViewModel<StartupShellModel>(), Nested: new RouteMap[]
			{
				new RouteMap("Login", View: views.FindByViewModel<StartupLoginModel>()),
				new RouteMap("Main", View: views.FindByViewModel<StartupMainModel>(), IsDefault: mainIsDefault),
			}));
	}

	/// <summary>
	/// A shell whose default page hosts a region with its own default child.
	/// </summary>
	private static void NestedRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap(ViewModel: typeof(StartupShellModel)),
			new ViewMap<StartupHostPage, StartupHostModel>(),
			new ViewMap<StartupChildPage, StartupChildModel>());

		routes.Register(
			new RouteMap("", View: views.FindByViewModel<StartupShellModel>(), Nested: new RouteMap[]
			{
				new RouteMap("Host", View: views.FindByViewModel<StartupHostModel>(), IsDefault: true, Nested: new RouteMap[]
				{
					new RouteMap("Child", View: views.FindByViewModel<StartupChildModel>(), IsDefault: true),
				}),
			}));
	}

	private async Task<StartupState> StartAsync<TExpectedPage>(
		Action<IViewRegistry, IRouteRegistry> registerRoutes,
		string? initialRoute = "",
		Type? initialView = null,
		Type? initialViewModel = null,
		Func<IServiceProvider, INavigator, Task>? initialNavigate = null,
		string launchUrl = "",
		[CallerMemberName] string testName = "")
		where TExpectedPage : Page
	{
		StartupCreations.Reset();
		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		var root = new ContentControl
		{
			HorizontalContentAlignment = HorizontalAlignment.Stretch,
			VerticalContentAlignment = VerticalAlignment.Stretch,
		};
		await UIHelper.Load(root);
		IHost? host = null;
		try
		{
			host = UnoHost
				.CreateDefaultBuilder(typeof(Given_StartupNavigation).Assembly)
				// On WebAssembly the host takes the browser address as the launch URL; pin it so every
				// platform starts the same way, and so the launch-route cases control it.
				.ConfigureHostConfiguration(config => config.AddInMemoryCollection(
					new Dictionary<string, string?> { [HostingConstants.LaunchUrlKey] = launchUrl }))
				.UseNavigation(viewRouteBuilder: registerRoutes)
				.Build();

			// The steps of Window.InitializeNavigationAsync, without Window.Activate(): the test window is
			// already active, and on WebAssembly activating it again throws.
			var services = await window.AttachServicesAsync(host.Services);
			var startup = root.HostAsync(services, initialRoute, initialView, initialViewModel, initialNavigate);
			await host.StartAsync();
			await startup.WaitAsync(Timeout);

			using var cts = new CancellationTokenSource(Timeout);
			try
			{
				await UIHelper.WaitFor(() => Visible(root).Page is TExpectedPage, cts.Token);
			}
			catch (TimeoutException)
			{
				throw new AssertFailedException(
					$"{typeof(TExpectedPage).Name} never came on screen. Visual chain: {Describe(root)}; created: {StartupCreations.Summary()}");
			}
			await UnitTestsUIContentHelper.WaitForIdle();

			var (page, backStackDepth) = Visible(root);
			var state = new StartupState(page, backStackDepth, StartupCreations.Summary());
			Console.WriteLine($"[startup-matrix] {testName}: visible={page?.GetType().Name ?? "(none)"} backStack={backStackDepth} created: {state.Created}");
			return state;
		}
		finally
		{
			if (host is not null)
			{
				await host.StopAsync();
			}
			UIHelper.Content = null;
		}
	}

	private static string Describe(ContentControl root)
	{
		var parts = new List<string>();
		object? current = root.Content;
		while (current is not null && parts.Count < 10)
		{
			parts.Add(current.GetType().Name);
			current = current switch
			{
				FrameView frameView => frameView.Content,
				StartupHostPage host => host.ChildRegion.Content,
				ContentControl contentControl => contentControl.Content,
				UserControl userControl => userControl.Content,
				_ => null,
			};
		}
		return parts.Count == 0 ? "(empty)" : string.Join(" > ", parts);
	}

	/// <summary>
	/// The innermost page on screen, and the back stack depth of the frame that shows it.
	/// </summary>
	private static (Page? Page, int BackStackDepth) Visible(ContentControl root)
	{
		Page? page = null;
		var backStackDepth = 0;
		object? current = root.Content;
		while (current is not null)
		{
			switch (current)
			{
				case FrameView frameView:
					current = frameView.Content;
					break;
				case Frame frame:
					backStackDepth = frame.BackStackDepth;
					current = frame.Content;
					break;
				case StartupHostPage host:
					page = host;
					current = host.ChildRegion.Content;
					break;
				case Page leaf:
					page = leaf;
					current = null;
					break;
				case ContentControl contentControl:
					current = contentControl.Content;
					break;
				default:
					current = null;
					break;
			}
		}
		return (page, backStackDepth);
	}

	private sealed record StartupState(Page? Page, int BackStackDepth, string Created)
	{
		public int Count<T>() => StartupCreations.Count<T>();

		public void ShouldShow<TPage>(int? backStackDepth)
			where TPage : Page
		{
			Page.Should().BeOfType<TPage>("that page should be on screen (created: {0})", Created);
			if (backStackDepth is { } expected)
			{
				BackStackDepth.Should().Be(expected, "nothing should be left to go back to (created: {0})", Created);
			}
		}

		public void ShouldHaveCreatedOnce<T>()
			=> Count<T>().Should().Be(1, "{0} should be constructed exactly once at startup (created: {1})", typeof(T).Name, Created);

		public void ShouldHaveCreatedAtMostOnce<T>()
			=> Count<T>().Should().BeLessThanOrEqualTo(1, "{0} should never be constructed twice at startup (created: {1})", typeof(T).Name, Created);
	}
}
