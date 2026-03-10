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

		AppTheme? receivedTheme = null;
		service.ThemeChanged += (_, theme) => receivedTheme = theme;

		// Act - Change to light; without XamlRoot, InternalSetThemeAsync will fail silently
		element.RequestedTheme = ElementTheme.Light;

		// Give time for any async event processing
		await Task.Delay(500);

		// Assert - ThemeChanged should NOT have fired with AppTheme.System
		// (Without XamlRoot, InternalSetThemeAsync fails, so no event fires at all,
		// but the key assertion is that the System shortcut path was not taken)
		receivedTheme.Should().NotBe(AppTheme.System,
			because: "when an explicit theme is saved, the system theme shortcut should not be taken");
	}
}
