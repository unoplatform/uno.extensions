using FluentAssertions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Toolkit;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Core.UI.Tests;

[TestClass]
[RunsOnUIThread]
public class Given_ThemeService
{
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

	[TestMethod]
	public async Task When_SavedThemeIsSystem_And_ActualThemeChanges_Then_ThemeChangedReportsSystem()
	{
		// Arrange
		var settings = new InMemorySettings();
		settings.Set("CurrentTheme", AppTheme.System.ToString());
		var dispatcher = new SynchronousDispatcher();
		var element = new Grid();
		element.RequestedTheme = ElementTheme.Light;

		using var service = new ThemeService(element, dispatcher, settings);

		var tcs = new TaskCompletionSource<AppTheme>();
		service.ThemeChanged += (_, theme) => tcs.TrySetResult(theme);

		// Act - Simulate OS theme change by switching RequestedTheme
		element.RequestedTheme = ElementTheme.Dark;

		using var cts = new CancellationTokenSource(DefaultTimeout);
		cts.Token.Register(() => tcs.TrySetCanceled());
		var receivedTheme = await tcs.Task;

		// Assert - should report System, not the specific dark/light value
		receivedTheme.Should().Be(AppTheme.System,
			because: "when following system theme, ThemeChanged should report System, not the actual dark/light value");
	}

	[TestMethod]
	public async Task When_SavedThemeIsSystem_And_ActualThemeChanges_Then_SavedThemeNotOverwritten()
	{
		// Arrange
		var settings = new InMemorySettings();
		settings.Set("CurrentTheme", AppTheme.System.ToString());
		var dispatcher = new SynchronousDispatcher();
		var element = new Grid();
		element.RequestedTheme = ElementTheme.Light;

		using var service = new ThemeService(element, dispatcher, settings);

		var tcs = new TaskCompletionSource<AppTheme>();
		service.ThemeChanged += (_, theme) => tcs.TrySetResult(theme);

		// Act
		element.RequestedTheme = ElementTheme.Dark;

		using var cts = new CancellationTokenSource(DefaultTimeout);
		cts.Token.Register(() => tcs.TrySetCanceled());
		await tcs.Task;

		// Assert - saved theme should remain "System", not be overwritten to "Dark"
		settings.Get("CurrentTheme").Should().Be(AppTheme.System.ToString(),
			because: "the saved theme should not be overwritten when following system theme");
	}

	[TestMethod]
	public async Task When_SavedThemeIsSystem_And_MultipleActualThemeChanges_Then_AllReportSystem()
	{
		// Arrange
		var settings = new InMemorySettings();
		settings.Set("CurrentTheme", AppTheme.System.ToString());
		var dispatcher = new SynchronousDispatcher();
		var element = new Grid();
		element.RequestedTheme = ElementTheme.Light;

		using var service = new ThemeService(element, dispatcher, settings);

		var receivedThemes = new List<AppTheme>();
		var tcs1 = new TaskCompletionSource<AppTheme>();
		var tcs2 = new TaskCompletionSource<AppTheme>();
		service.ThemeChanged += (_, theme) =>
		{
			receivedThemes.Add(theme);
			if (receivedThemes.Count == 1) tcs1.TrySetResult(theme);
			if (receivedThemes.Count == 2) tcs2.TrySetResult(theme);
		};

		using var cts = new CancellationTokenSource(DefaultTimeout);
		cts.Token.Register(() => { tcs1.TrySetCanceled(); tcs2.TrySetCanceled(); });

		// Act - First theme change
		element.RequestedTheme = ElementTheme.Dark;
		await tcs1.Task;

		// Second theme change
		element.RequestedTheme = ElementTheme.Light;
		await tcs2.Task;

		// Assert - all events should report System, not Dark/Light
		receivedThemes.Should().HaveCount(2);
		receivedThemes.Should().OnlyContain(t => t == AppTheme.System,
			because: "repeated OS theme changes should continue reporting System when following system theme");
	}

	[TestMethod]
	public async Task When_SavedThemeIsExplicit_And_ActualThemeChanges_Then_DoesNotReportSystem()
	{
		// Arrange - saved theme is explicitly Dark, not System
		var settings = new InMemorySettings();
		settings.Set("CurrentTheme", AppTheme.Dark.ToString());
		var dispatcher = new SynchronousDispatcher();
		var element = new Grid();
		element.RequestedTheme = ElementTheme.Dark;

		using var service = new ThemeService(element, dispatcher, settings);

		var tcs = new TaskCompletionSource<AppTheme>();
		service.ThemeChanged += (_, theme) => tcs.TrySetResult(theme);

		// Act - Change to light; without XamlRoot, InternalSetThemeAsync will fail silently
		element.RequestedTheme = ElementTheme.Light;

		// Assert - Without XamlRoot, InternalSetThemeAsync fails silently so no event fires.
		// The key verification is that the System shortcut path is NOT taken for explicit themes.
		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
		cts.Token.Register(() => tcs.TrySetCanceled());

		var eventFired = false;
		try
		{
			var receivedTheme = await tcs.Task;
			eventFired = true;
			receivedTheme.Should().NotBe(AppTheme.System,
				because: "when an explicit theme is saved, the system theme shortcut should not be taken");
		}
		catch (TaskCanceledException)
		{
			// Expected: no event fires because InternalSetThemeAsync cannot succeed without XamlRoot
		}

		eventFired.Should().BeFalse(
			because: "without a XamlRoot, InternalSetThemeAsync fails silently and ThemeChanged should not fire");
	}

	[TestMethod]
	public async Task When_HostedUnderForeignXamlRoot_Then_ThemeAppliesToOwnRoot_NotHost()
	{
		// Regression guard for #3120: the theme must be applied to the service's own root element,
		// not to RootElement.XamlRoot.Content (the root visual of the XamlRoot). The host owns the
		// XamlRoot and the app root is a nested child — mirroring a secondary app whose Window.Content
		// is re-parented into a host's shared XamlRoot (e.g. an app loaded into a collectible ALC).
		using var cts = new CancellationTokenSource(DefaultTimeout);
		var ct = cts.Token;

		var settings = new InMemorySettings();
		settings.Set("CurrentTheme", AppTheme.Light.ToString());
		var dispatcher = new SynchronousDispatcher();

		var hostRoot = new Grid { RequestedTheme = ElementTheme.Default };
		var appRoot = new Grid();
		hostRoot.Children.Add(appRoot);

		UnitTestsUIContentHelper.SaveOriginalContent();
		try
		{
			UnitTestsUIContentHelper.CurrentTestWindow!.Content = hostRoot;
			await UIHelper.WaitFor(() => appRoot.XamlRoot is not null, ct);

			// Precondition: the app root is NOT the XamlRoot's content — the host is.
			appRoot.XamlRoot!.Content.Should().BeSameAs(hostRoot,
				because: "the host, not the hosted app, owns the XamlRoot in this topology");

			using var service = new ThemeService(appRoot, dispatcher, settings);
			await service.InitializeAsync();

			var changed = default(AppTheme?);
			service.ThemeChanged += (_, theme) => changed = theme;

			// Act
			var result = await service.SetThemeAsync(AppTheme.Dark);

			// Assert: the service's own root is themed; the host that owns the XamlRoot is untouched.
			result.Should().BeTrue();
			appRoot.RequestedTheme.Should().Be(ElementTheme.Dark,
				because: "the theme must apply to the service's own root element");
			hostRoot.RequestedTheme.Should().Be(ElementTheme.Default,
				because: "the host that owns the XamlRoot must not be re-themed by a hosted app");
			changed.Should().Be(AppTheme.Dark,
				because: "ThemeChanged must fire when the effective theme flips");
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}
}
