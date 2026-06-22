using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.UI;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Specs for the navigation boundary: an element with <c>Region.Attached="false"</c> set as a local
/// value (and not overridden to true) stops the upward service-provider walk (ServiceForControl), so
/// its subtree is detached from the navigation above it. This lets a host isolate a hosted subtree
/// without it self-wiring into the surrounding route graph.
///
/// The responsive master-detail idiom (local <c>Region.Attached="false"</c> flipped to true by a
/// VisualState setter for the wide layout) must NOT become a boundary; that exclusion is verified
/// end-to-end by Given_Responsive in the TestHarness UI tests, and at the predicate level here by
/// <see cref="When_AttachedExplicitlyTrue_Then_NotABoundary"/>.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_NavigationBoundary
{
	// root (holds the service provider) -> mid (the boundary candidate) -> leaf (resolves upward).
	private async Task<(Grid Root, Grid Mid, Grid Leaf, IServiceProvider? Sp)> BuildLoadedTreeAsync(CancellationToken ct, bool attachServiceProvider = true)
	{
		var leaf = new Grid();
		var mid = new Grid();
		mid.Children.Add(leaf);
		var root = new Grid();
		root.Children.Add(mid);

		IServiceProvider? sp = null;
		if (attachServiceProvider)
		{
			sp = new ServiceCollection().BuildServiceProvider();
			root.SetServiceProvider(sp);
		}

		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		UnitTestsUIContentHelper.SaveOriginalContent();
		window.Content = root;

		// Wait until the tree is realized so VisualTreeHelper.GetParent works during resolution.
		await UIHelper.WaitFor(() => VisualTreeHelper.GetParent(leaf) is not null, ct);

		return (root, mid, leaf, sp);
	}

	[TestMethod]
	public async Task When_NoBoundary_Then_LeafResolvesServiceProvider()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
		try
		{
			var (_, _, leaf, sp) = await BuildLoadedTreeAsync(ct);

			// Untouched tree: the walk passes through mid up to root and finds the service provider.
			leaf.FindServiceProvider().Should().BeSameAs(sp);
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}

	[TestMethod]
	public async Task When_AttachedExplicitlyFalse_Then_WalkStopsAtBoundary()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
		try
		{
			var (_, mid, leaf, _) = await BuildLoadedTreeAsync(ct);

			mid.SetAttached(false); // explicit local false marks mid as a navigation boundary

			// The walk stops at the boundary and does not reach the service provider above it.
			leaf.FindServiceProvider().Should().BeNull("the walk must stop at the boundary and not reach the service provider above it");
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}

	[TestMethod]
	public async Task When_AttachedDefault_Then_NotABoundary()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
		try
		{
			var (_, mid, leaf, sp) = await BuildLoadedTreeAsync(ct);

			// Regression guard for the ReadLocalValue subtlety: the default (unset) false that every
			// element has must NOT be treated as a boundary.
			mid.GetAttached().Should().BeFalse();
			leaf.FindServiceProvider().Should().BeSameAs(sp);
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}

	[TestMethod]
	public async Task When_AttachedExplicitlyTrue_Then_NotABoundary()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
		try
		{
			var (_, mid, leaf, sp) = await BuildLoadedTreeAsync(ct);

			// An effective-true Region.Attached (a real region) is not a boundary, so resolution
			// still reaches the provider above. (The local-false-but-effective-true VisualState flip
			// of the wide responsive layout is exercised end-to-end by Given_Responsive.)
			mid.SetAttached(true);

			leaf.FindServiceProvider().Should().BeSameAs(sp);
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}

	[TestMethod]
	public async Task When_NoServiceProviderAndNoBoundary_Then_NotReportedAsBoundary()
	{
		var ct = new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token;
		try
		{
			// No service provider anywhere and no boundary: resolution simply returns null (and the
			// root navigator legitimately warns) - the boundary check must not false-trigger here.
			var (_, _, leaf, _) = await BuildLoadedTreeAsync(ct, attachServiceProvider: false);

			leaf.FindServiceProvider().Should().BeNull();
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}
}
