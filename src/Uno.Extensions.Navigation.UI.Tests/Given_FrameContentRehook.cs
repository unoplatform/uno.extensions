using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Hosting;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Navigators;
using Uno.Extensions.Navigation.UI.Controls;
using Uno.Extensions.Navigation.UI.Tests.Pages;
using Uno.Extensions.Navigation.UI.Tests.ViewModels;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Tests for <see cref="FrameNavigator.RehookCurrentViewAfterHotReload"/> — the recovery
/// path for uno.extensions#3130, where Uno's hot-reload element update replaces
/// <c>Frame.Content</c> directly (no <c>Frame.Navigated</c> is raised) and copies the
/// locally-set DataContext to the replacement instance. The swap is simulated by
/// assigning <c>Frame.Content</c>, so these tests run without the hot-reload harness
/// and pin the view-model refresh decision deterministically:
/// a delta that did NOT update the mapped view model preserves the copied DataContext
/// (and any un-persisted state it holds); a delta that did update it — or a replacement
/// whose DataContext is missing — rebuilds the view model through the standard pipeline.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_FrameContentRehook
{
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

	private async Task<(IHost Host, Frame Frame, FrameNavigator FrameNavigator)> SetupNavigationAsync()
	{
		var window = new Window();

		var host = await window.InitializeNavigationAsync(
			buildHost: async () => UnoHost
				.CreateDefaultBuilder(typeof(Given_FrameContentRehook).Assembly)
				.UseNavigation(viewRouteBuilder: (views, routes) =>
				{
					views.Register(
						new ViewMap<HotReloadVmPage, HotReloadVm>());

					routes.Register(
						new RouteMap("", Nested: new RouteMap[]
						{
							new RouteMap("HotReloadVmPage", View: views.FindByView<HotReloadVmPage>(), IsDefault: true),
						}));
				})
				.Build(),
			initialRoute: "HotReloadVmPage");

		var root = (ContentControl)window.Content!;

		using var cts = new CancellationTokenSource(Timeout);
		await UIHelper.WaitFor(
			() => root.Content is FrameView fv &&
				fv.FindName("NavigationFrame") is Frame frame &&
				frame.Content is HotReloadVmPage page &&
				page.DataContext is HotReloadVm,
			cts.Token);

		var frameView = (FrameView)root.Content!;
		var frame = (Frame)frameView.FindName("NavigationFrame");
		var frameNavigator = frameView.Navigator as FrameNavigator;
		frameNavigator.Should().NotBeNull("the FrameView's navigator must be the inner Frame's FrameNavigator");

		return (host, frame, frameNavigator!);
	}

	/// <summary>
	/// Simulates Uno's frame element-update handler: a fresh instance of the page type,
	/// the locally-set DataContext copied over (when provided), and <c>Frame.Content</c>
	/// patched directly without raising <c>Frame.Navigated</c>.
	/// </summary>
	private static HotReloadVmPage SwapFrameContent(Frame frame, object? copiedDataContext)
	{
		var replacement = new HotReloadVmPage();
		if (copiedDataContext is not null)
		{
			replacement.DataContext = copiedDataContext;
		}
		frame.Content = replacement;
		return replacement;
	}

	[TestMethod]
	public async Task When_XamlOnlyDelta_Then_CopiedViewModelIsPreserved()
	{
		var (host, frame, frameNavigator) = await SetupNavigationAsync();

		try
		{
			var originalVm = (HotReloadVm)((HotReloadVmPage)frame.Content).DataContext;

			var replacement = SwapFrameContent(frame, originalVm);

			// XAML-only delta: only the page type was updated, not the view model.
			frameNavigator.RehookCurrentViewAfterHotReload(new[] { typeof(HotReloadVmPage) });

			// The re-hook itself is synchronous: the replacement must be adopted as the
			// region's named view immediately.
			Uno.Extensions.Navigation.UI.Region.GetName(replacement).Should().Be("HotReloadVmPage",
				"the re-hook must re-apply the region name to the replacement instance");

			// The view-model refresh is deferred onto the dispatcher; give it time to run,
			// then assert it did NOT discard the copied view model.
			await Task.Delay(500);

			replacement.DataContext.Should().BeSameAs(originalVm,
				"a delta that did not update the mapped view model must preserve the copied " +
				"DataContext — recreating it would discard un-persisted view-model state");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	[TestMethod]
	public async Task When_ViewModelInDelta_Then_ViewModelIsRebuilt()
	{
		var (host, frame, frameNavigator) = await SetupNavigationAsync();

		try
		{
			var originalVm = (HotReloadVm)((HotReloadVmPage)frame.Content).DataContext;

			var replacement = SwapFrameContent(frame, originalVm);

			// The delta contains the mapped view-model type (an in-place EnC update keeps
			// the type identity, so the copied DataContext still type-checks as valid).
			frameNavigator.RehookCurrentViewAfterHotReload(new[] { typeof(HotReloadVmPage), typeof(HotReloadVm) });

			using var cts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => replacement.DataContext is HotReloadVm vm && !ReferenceEquals(vm, originalVm),
				cts.Token);

			replacement.DataContext.Should().BeOfType<HotReloadVm>(
				"a delta that updated the mapped view model must rebuild it so bindings target " +
				"an instance built from the updated type");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	[TestMethod]
	public async Task When_ReplacementHasNoDataContext_Then_ViewModelIsCreated()
	{
		var (host, frame, frameNavigator) = await SetupNavigationAsync();

		try
		{
			var originalVm = (HotReloadVm)((HotReloadVmPage)frame.Content).DataContext;

			// The #3130 shape: the replacement carries no usable DataContext, and the delta
			// does not name the view model — the missing-VM check must still recover it.
			var replacement = SwapFrameContent(frame, copiedDataContext: null);

			frameNavigator.RehookCurrentViewAfterHotReload(new[] { typeof(HotReloadVmPage) });

			using var cts = new CancellationTokenSource(Timeout);
			await UIHelper.WaitFor(
				() => replacement.DataContext is HotReloadVm,
				cts.Token);

			replacement.DataContext.Should().BeOfType<HotReloadVm>(
				"a replacement without a DataContext must get a view model built through the " +
				"standard pipeline, otherwise bound content renders empty (#3130)");
			replacement.DataContext.Should().NotBeSameAs(originalVm,
				"the view model is rebuilt for the replacement instance rather than rebound " +
				"to the dead page's instance");
		}
		finally
		{
			await host.StopAsync();
		}
	}

	[TestMethod]
	public async Task When_ContentUnchanged_Then_RehookIsNoOp()
	{
		var (host, frame, frameNavigator) = await SetupNavigationAsync();

		try
		{
			var page = (HotReloadVmPage)frame.Content;
			var originalVm = (HotReloadVm)page.DataContext;

			// No external swap happened — the re-hook must leave everything untouched even
			// when the delta names the view model (mismatch check gates the whole path).
			frameNavigator.RehookCurrentViewAfterHotReload(new[] { typeof(HotReloadVm) });

			await Task.Delay(500);

			frame.Content.Should().BeSameAs(page);
			page.DataContext.Should().BeSameAs(originalVm,
				"without a content swap there is nothing to re-hook, so the live view model must not be touched");
		}
		finally
		{
			await host.StopAsync();
		}
	}
}
