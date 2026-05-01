using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation.UI;

namespace Uno.Extensions.Navigation.UI.Tests.Pages;

/// <summary>
/// Host page for the inline-Panel HR test. Implements the canonical
/// <c>HowTo-UsePanel</c> shape: a <see cref="Grid"/> region container with three
/// pre-existing child Grids identified by <c>Region.Name</c>, switched via
/// <see cref="Uno.Extensions.Navigation.Navigators.PanelVisiblityNavigator"/>
/// (<c>Region.Navigator="Visibility"</c>). Unlike <see cref="HotReloadRegionPage"/>,
/// the panel is NOT empty — its children pre-exist, so the navigator just toggles
/// <see cref="UIElement.Visibility"/> rather than materializing FrameViews. This is
/// the path most apps take when using a Panel to switch sections without a Frame.
/// <para>
/// Each named child (<see cref="RegionOne"/>/<see cref="RegionTwo"/>/<see cref="RegionThree"/>)
/// registers a property-changed callback on its own <see cref="UIElement.VisibilityProperty"/>.
/// Whenever the child transitions to <see cref="Visibility.Visible"/>, it re-reads
/// <see cref="HotReloadPanelTarget.GetValue"/> into its TextBlock — the canonical
/// "refresh on show" pattern. That re-read is what makes a hot-reload edit to
/// <c>HotReloadPanelTarget.GetValue</c> observable on the next region switch even
/// though the inline child instances themselves are reused.
/// </para>
/// </summary>
public sealed partial class HotReloadPanelHostPage : Page
{
	public Grid PanelRoot { get; }

	public InlineRegion RegionOne { get; }

	public InlineRegion RegionTwo { get; }

	public InlineRegion RegionThree { get; }

	public HotReloadPanelHostPage()
	{
		RegionOne = new InlineRegion("One");
		RegionTwo = new InlineRegion("Two");
		RegionThree = new InlineRegion("Three");

		PanelRoot = new Grid();
		Region.SetAttached(PanelRoot, true);
		Region.SetNavigator(PanelRoot, "Visibility");
		PanelRoot.Children.Add(RegionOne);
		PanelRoot.Children.Add(RegionTwo);
		PanelRoot.Children.Add(RegionThree);

		Content = PanelRoot;
	}

	/// <summary>
	/// A pre-existing inline panel child carrying a region name and a TextBlock that
	/// re-reads the HR target on every Collapsed→Visible transition.
	/// </summary>
	public sealed partial class InlineRegion : Grid
	{
		public TextBlock Text { get; }

		public InlineRegion(string regionName)
		{
			Region.SetName(this, regionName);
			Visibility = Visibility.Collapsed;
			Text = new TextBlock();
			Children.Add(Text);
			RegisterPropertyChangedCallback(VisibilityProperty, OnVisibilityChanged);
		}

		private void OnVisibilityChanged(DependencyObject sender, DependencyProperty dp)
		{
			if (Visibility == Visibility.Visible)
			{
				Text.Text = HotReloadPanelTarget.GetValue();
			}
		}
	}
}
