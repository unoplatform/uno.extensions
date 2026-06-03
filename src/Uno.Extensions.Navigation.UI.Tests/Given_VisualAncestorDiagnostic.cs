using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Navigation;
using Uno.Extensions.Navigation.Regions;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Navigation.UI.Tests;

/// <summary>
/// Tests for <see cref="NavigationRegion.BuildVisualAncestorDiagnostic(FrameworkElement?)"/>, the
/// diagnostic emitted when a region cannot resolve a root service provider. The chain must reveal
/// whether the navigation root (the ancestor carrying <c>Region.ServiceProvider</c>) is present —
/// which distinguishes a timing failure (root present → re-driving AssignParent recovers) from a
/// structural one (root absent → the content is attached outside the navigation root).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_VisualAncestorDiagnostic
{
	[TestMethod]
	public void When_StartIsNull_Then_ReturnsNoView()
	{
		NavigationRegion.BuildVisualAncestorDiagnostic(null).Should().Be("(no view)");
	}

	[TestMethod]
	public async Task When_ServiceProviderOnAncestor_Then_ChainMarksSp()
	{
		// Arrange — a loaded tree: Grid#RootHost [sp] > Border. Mirrors the navigation root
		// (which carries Region.ServiceProvider) sitting above a region's view.
		var grid = new Grid { Name = "RootHost" };
		var border = new Border();
		grid.Children.Add(border);

		var sp = new ServiceCollection().BuildServiceProvider();
		grid.SetServiceProvider(sp);

		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		var loaded = new TaskCompletionSource();
		border.Loaded += (_, _) => loaded.TrySetResult();

		UnitTestsUIContentHelper.SaveOriginalContent();
		try
		{
			window.Content = grid;
			await loaded.Task.WaitAsync(TimeSpan.FromSeconds(30));

			// Act
			var chain = NavigationRegion.BuildVisualAncestorDiagnostic(border);

			// Assert — the walk reaches the SP-bearing ancestor and flags it.
			chain.Should().StartWith("Border");
			chain.Should().Contain("Grid#RootHost");
			chain.Should().Contain("[sp]");
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}

	[TestMethod]
	public async Task When_NoServiceProviderInChain_Then_NoSpMarker()
	{
		// Arrange — a loaded tree with NO Region.ServiceProvider anywhere. Mirrors the blank-frame
		// case where the region is attached outside the navigation root.
		var grid = new Grid { Name = "Detached" };
		var border = new Border();
		grid.Children.Add(border);

		var window = UnitTestsUIContentHelper.CurrentTestWindow!;
		var loaded = new TaskCompletionSource();
		border.Loaded += (_, _) => loaded.TrySetResult();

		UnitTestsUIContentHelper.SaveOriginalContent();
		try
		{
			window.Content = grid;
			await loaded.Task.WaitAsync(TimeSpan.FromSeconds(30));

			// Act
			var chain = NavigationRegion.BuildVisualAncestorDiagnostic(border);

			// Assert — chain is produced but no ancestor is flagged with a service provider.
			chain.Should().StartWith("Border");
			chain.Should().NotContain("[sp]");
		}
		finally
		{
			UnitTestsUIContentHelper.RestoreOriginalContent();
		}
	}
}
