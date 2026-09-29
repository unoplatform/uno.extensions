using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Navigation.UI.Tests.Pages;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Probe for unoplatform/uno.extensions#3130: verifies that a page navigated into a Frame
/// whose ancestor is Collapsed exists only as <c>Frame.Content</c> and is NOT a materialized
/// visual child. This is the exact state the WASM repro proved (the app view gets no layout
/// pass during generation), and the state Uno's hot-reload visual-tree walk cannot see.
/// If these assertions hold on Skia desktop, the stranded-default-page bug can be reproduced
/// deterministically on desktop by forcing this state.
/// </summary>
[TestClass]
[RunsInSecondaryApp(ignoreIfNotSupported: true)]
public class Given_UnmaterializedFrameContent
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_FrameUnderCollapsedAncestor_Then_NavigatedContentIsNotMaterialized()
	{
		var collapsedHost = new Grid { Visibility = Visibility.Collapsed };
		var frame = new Frame();
		collapsedHost.Children.Add(frame);

		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		UnitTestsUIContentHelper.SaveOriginalContent();
		try
		{
			window.Content = collapsedHost;
			await Task.Delay(200);

			frame.Navigate(typeof(TestPageOne));

			// Generous settle: give any async materialization a chance to happen so the
			// "not materialized" assertion below is about steady state, not a race.
			await Task.Delay(1000);

			if (frame.Content is not TestPageOne page)
			{
				Assert.Fail("Frame.Navigate must set Frame.Content even without a layout pass");
				return;
			}

			var descendants = Descendants(frame).ToArray();
			var pageMaterialized = descendants.Contains(page);

			pageMaterialized.Should().BeFalse(
				"PROBE: a page under a Collapsed ancestor should exist only as Frame.Content, not as a " +
				$"visual child (frame.IsLoaded={frame.IsLoaded}, page.IsLoaded={page.IsLoaded}, " +
				$"frame visual descendants=[{string.Join(", ", descendants.Select(d => d.GetType().Name))}])");

			// Sanity half: making the ancestor visible must materialize and load the page.
			collapsedHost.Visibility = Visibility.Visible;

			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
			while (!Descendants(frame).Contains(page))
			{
				cts.Token.ThrowIfCancellationRequested();
				await Task.Delay(50, cts.Token);
			}

			await Task.Delay(200);
			page.IsLoaded.Should().BeTrue("once the ancestor is visible the page must load");
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}

	private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
	{
		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			yield return child;
			foreach (var grandChild in Descendants(child))
			{
				yield return grandChild;
			}
		}
	}
}
