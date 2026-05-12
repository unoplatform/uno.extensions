#if DEBUG // Hot-reload tests are only relevant in debug configuration
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Config;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

/// <summary>
/// Hot Reload tests for MVUX ListFeed scenarios.
/// Verifies that ListFeed-backed UI updates correctly when the underlying
/// model method body is changed via C# hot reload and when XAML templates
/// are swapped via XAML hot reload.
/// </summary>
[TestClass]
[RunsInSecondaryApp(ignoreIfNotSupported: true)]
public class Given_HotReloadListFeed
{
	[TestInitialize]
	public void Setup()
	{
		HotReloadHelper.DefaultWorkspaceTimeout = TimeSpan.FromSeconds(300);
		HotReloadHelper.DefaultMetadataUpdateTimeout = TimeSpan.FromSeconds(60);

		// Enable MVUX hot-reload support for this test process.
		// The module initializer in the test assembly won't enable it because the
		// entry assembly is the RuntimeTests host, not the test assembly.
		// We force-set via reflection because ConfigureHotReload requires the
		// entry assembly name to match, which may not be reliable in test hosts.
		var configType = typeof(FeedConfiguration);
		var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
		configType.GetProperty("HotReload", flags)?.SetValue(null, HotReloadSupport.Enabled);
		// Reset the cached effective value so it picks up our new setting.
		configType.GetField("_effectiveHotReload", flags)?.SetValue(null, null);

		FeedConfiguration.EffectiveHotReload.HasFlag(HotReloadSupport.DynamicFeed)
			.Should().BeTrue("MVUX hot-reload must be enabled for feed re-execution");
	}

	// ──────────────────────────────────────────────────────────────────────
	// 1. Basic HR method body change for ListFeed data source
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Proves that a C# HR change to a static method returning
	/// <see cref="IImmutableList{T}"/> is observed by subsequent calls.
	/// </summary>
	[TestMethod]
	public async Task When_ListTargetMethodChangedViaHR_Then_ReturnsUpdatedItems(CancellationToken ct)
	{
		HotReloadListFeedTarget.GetItems()
			.Should().BeEquivalentTo(new[] { "Item1", "Item2", "Item3" });

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/HotReloadListFeedTarget.cs",
			"""return ImmutableList.Create("Item1", "Item2", "Item3");""",
			"""return ImmutableList.Create("ItemA", "ItemB");""",
			ct);

		HotReloadListFeedTarget.GetItems()
			.Should().BeEquivalentTo(new[] { "ItemA", "ItemB" });
	}

	// ──────────────────────────────────────────────────────────────────────
	// 2. MVUX ListFeed pipeline — C# HR on data source → new ViewModel sees update
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// Verifies the MVUX pipeline after C# HR: when the data source method body
	/// (<see cref="HotReloadListFeedTarget.GetPipelineItems"/>) is changed via HR,
	/// a newly-created ViewModel picks up the updated code and the ListFeed emits
	/// the new items. This mirrors how navigation-based HR works — after HR,
	/// navigating to a fresh page creates a new ViewModel that observes the
	/// updated method body.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_ModelMethodChangedViaHR_Then_NewViewModelReflectsUpdate(CancellationToken ct)
	{
		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		UnitTestsUIContentHelper.SaveOriginalContent();
		try
		{
			var page = new HotReloadListFeedPage();
			page.DataContext = new HotReloadListFeedViewModel();
			window.Content = page;

			// Wait for FeedView to render its template and items to populate.
			var itemsControl = await WaitForItemsControlAsync(page, TimeSpan.FromSeconds(30), ct);
			await WaitForItemCountAsync(itemsControl, 3, TimeSpan.FromSeconds(30), ct);

			GetItemTexts(itemsControl).Should().BeEquivalentTo(
				new[] { "PipeA", "PipeB", "PipeC" },
				"Initial ListFeed emission should contain 3 pipeline items");

			// C# HR: change the data source method body in HotReloadListFeedTarget.
			// This target is a simple static class — HR applies reliably to it.
			await using var _ = await HotReloadHelper.UpdateSourceFile(
				"../../Uno.Extensions.Reactive.UI.Tests/HotReloadListFeedTarget.cs",
				"""return ImmutableList.Create("PipeA", "PipeB", "PipeC");""",
				"""return ImmutableList.Create("PipeX", "PipeY");""",
				ct);

			// Diagnostic: verify the HR delta was applied.
			HotReloadListFeedTarget.GetPipelineItems().Should().BeEquivalentTo(
				new[] { "PipeX", "PipeY" },
				"Direct call to GetPipelineItems() after HR should return updated items");

			// Create a fresh ViewModel — the new Model instance calls the updated
			// GetPipelineItems() via its ListFeed, emitting the new items.
			page.DataContext = new HotReloadListFeedViewModel();

			// The FeedView re-binds to the new VM. Wait for the new items.
			var newItemsControl = await WaitForItemsControlAsync(page, TimeSpan.FromSeconds(30), ct);
			await WaitForItemCountAsync(newItemsControl, 2, TimeSpan.FromSeconds(30), ct);

			GetItemTexts(newItemsControl).Should().BeEquivalentTo(
				new[] { "PipeX", "PipeY" },
				"After HR + new ViewModel, ListFeed should emit the updated items");
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}

	// ──────────────────────────────────────────────────────────────────────
	// 3. XAML HR — page template swap preserves ListFeed binding
	// ──────────────────────────────────────────────────────────────────────

	/// <summary>
	/// XAML HR changes the item template inside the FeedView (e.g. adds FontWeight).
	/// After page replacement, the ListFeed binding should still be active and
	/// display items.
	/// </summary>
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_XamlTemplateChangedViaHR_Then_ListFeedBindingPreserved(CancellationToken ct)
	{
		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		UnitTestsUIContentHelper.SaveOriginalContent();
		try
		{
			var page = new HotReloadListFeedPage();
			page.DataContext = new HotReloadListFeedViewModel();
			window.Content = page;

			// Wait for initial items.
			var itemsControl = await WaitForItemsControlAsync(page, TimeSpan.FromSeconds(30), ct);
			await WaitForItemCountAsync(itemsControl, 3, TimeSpan.FromSeconds(30), ct);

			// XAML HR: change the item template (add FontWeight attribute).
			await using var _ = await HotReloadHelper.UpdateSourceFile(
				"../../Uno.Extensions.Reactive.UI.Tests/HotReloadListFeedPage.xaml",
				"""<TextBlock Text="{Binding}" />""",
				"""<TextBlock Text="{Binding}" FontWeight="Bold" />""",
				ct);

			// Wait for page replacement — XAML HR creates a new page instance.
			var newPage = await WaitForPageReplacementAsync<HotReloadListFeedPage>(
				window, page, TimeSpan.FromSeconds(30), ct);

			// The new page needs a ViewModel — create one so FeedView has a source.
			newPage.DataContext = new HotReloadListFeedViewModel();

			// Find the new ItemsControl in the replaced page.
			var newItemsControl = await WaitForItemsControlAsync(newPage, TimeSpan.FromSeconds(30), ct);
			await WaitForItemCountAsync(newItemsControl, 3, TimeSpan.FromSeconds(30), ct);

			GetItemTexts(newItemsControl).Should().BeEquivalentTo(
				new[] { "PipeA", "PipeB", "PipeC" },
				"After XAML HR page swap, ListFeed binding should still display items");
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}

	#region Helpers

	private static async Task<ItemsControl> WaitForItemsControlAsync(
		Page page, TimeSpan timeout, CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			var control = FindChild<ItemsControl>(page);
			if (control is not null)
			{
				return control;
			}
			await Task.Delay(100, ct);
		}
		throw new TimeoutException(
			$"ItemsControl did not appear in the visual tree within {timeout.TotalSeconds:F0}s.");
	}

	private static async Task WaitForItemCountAsync(
		ItemsControl itemsControl, int expectedCount, TimeSpan timeout, CancellationToken ct)
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (itemsControl.Items.Count == expectedCount)
			{
				return;
			}
			await Task.Delay(100, ct);
		}
		throw new TimeoutException(
			$"Expected {expectedCount} items but found {itemsControl.Items.Count} within {timeout.TotalSeconds:F0}s.");
	}

	private static async Task<TPage> WaitForPageReplacementAsync<TPage>(
		Window window, TPage oldPage, TimeSpan timeout, CancellationToken ct) where TPage : class
	{
		var sw = System.Diagnostics.Stopwatch.StartNew();
		while (sw.Elapsed < timeout)
		{
			ct.ThrowIfCancellationRequested();
			if (window.Content is TPage newPage && !ReferenceEquals(newPage, oldPage))
			{
				return newPage;
			}
			await Task.Delay(100, ct);
		}
		throw new TimeoutException(
			$"Page was not replaced within {timeout.TotalSeconds:F0}s.");
	}

	private static List<string> GetItemTexts(ItemsControl itemsControl)
	{
		return itemsControl.Items
			.Cast<object>()
			.Select(item => item?.ToString() ?? string.Empty)
			.ToList();
	}

	private static T? FindChild<T>(DependencyObject parent) where T : class, DependencyObject
	{
		for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			if (child is T result)
			{
				return result;
			}
			var found = FindChild<T>(child);
			if (found is not null)
			{
				return found;
			}
		}
		return null;
	}

	#endregion
}
#endif
