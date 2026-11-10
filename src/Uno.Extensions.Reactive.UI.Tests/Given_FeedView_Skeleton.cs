using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.UI;
using Uno.Toolkit.UI;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

/// <summary>
/// Tests for the <c>SkeletonFeedViewStyle</c>: a skeleton derived from the ValueTemplate while the FeedView loads,
/// and placeholders overlaid on the live content while it refreshes.
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_FeedView_Skeleton : FeedTests
{
	private const string SkeletonFeedViewStyle = nameof(SkeletonFeedViewStyle);
	private const string InitialSkeleton = nameof(InitialSkeleton);
	private const string SomePresenter = nameof(SomePresenter);
	private static readonly Uri FeedViewResources = new("ms-appx:///Uno.Extensions.Reactive.UI/View/FeedView.xaml");

	private static readonly string[] People = ["John", "Jane", "Alex"];

	[TestMethod]
	public async Task When_Loading_Then_ValueTemplateSkeletonShown()
	{
		var sut = await Setup(new Loader());

		var initial = GetPart<SkeletonPresenter>(sut, InitialSkeleton);
		initial.IsLoading.Should().BeTrue();
		initial.Visibility.Should().Be(Visibility.Visible);
		GetPart<SkeletonView>(sut, SomePresenter).Visibility.Should().Be(Visibility.Collapsed);

		await UIHelper.WaitFor(() => GetListView(initial) is { Items.Count: SkeletonPresenter.DefaultPlaceholderCount }, CT);
	}

	[TestMethod]
	public async Task When_PlaceholderCountSetOnFeedView_Then_SkeletonHasThatManyRows()
	{
		var sut = await Setup(new Loader(), placeholderCount: 6);

		var initial = GetPart<SkeletonPresenter>(sut, InitialSkeleton);

		await UIHelper.WaitFor(() => GetListView(initial) is { Items.Count: 6 }, CT);
	}

	[TestMethod]
	public async Task When_Loaded_Then_ValueShownAndSkeletonHidden()
	{
		var loader = new Loader();
		var sut = await Setup(loader);
		var initial = GetPart<SkeletonPresenter>(sut, InitialSkeleton);
		var some = GetPart<SkeletonView>(sut, SomePresenter);

		await CompleteLoad(loader, some, People);

		initial.IsLoading.Should().BeFalse();
		initial.Visibility.Should().Be(Visibility.Collapsed);
		some.IsLoading.Should().BeFalse("the live content is only covered while refreshing");
	}

	[TestMethod]
	public async Task When_Refreshed_Then_LiveContentCoveredUntilLoaded()
	{
		var loader = new Loader();
		var sut = await Setup(loader);
		var initial = GetPart<SkeletonPresenter>(sut, InitialSkeleton);
		var some = GetPart<SkeletonView>(sut, SomePresenter);
		await CompleteLoad(loader, some, People);

		loader.Reset();
		sut.Refresh.Execute(null);

		await UIHelper.WaitFor(() => some.IsLoading, CT);
		some.Visibility.Should().Be(Visibility.Visible, "the live content stays in place under the placeholders");
		initial.Visibility.Should().Be(Visibility.Collapsed, "a refresh doesn't replace the live content with the template skeleton");

		await CompleteLoad(loader, some, ["Sam"]);
		await UIHelper.WaitFor(() => !some.IsLoading, CT);
	}

	[TestMethod]
	public async Task When_SourceChanged_Then_ValueTemplateSkeletonShownAgain()
	{
		var loader = new Loader();
		var sut = await Setup(loader);
		var initial = GetPart<SkeletonPresenter>(sut, InitialSkeleton);
		var some = GetPart<SkeletonView>(sut, SomePresenter);
		await CompleteLoad(loader, some, People);

		var reloader = new Loader();
		sut.Source = reloader.Feed;

		await UIHelper.WaitFor(() => initial is { IsLoading: true, Visibility: Visibility.Visible }, CT);

		await CompleteLoad(reloader, some, ["Sam"]);
		await UIHelper.WaitFor(() => initial.Visibility is Visibility.Collapsed, CT);
	}

	private async Task CompleteLoad(Loader loader, SkeletonView some, string[] people)
	{
		loader.Complete(people);
		await UIHelper.WaitFor(() => some.Visibility is Visibility.Visible && GetListView(some)?.Items.Cast<string>().SequenceEqual(people) is true, CT);
	}

	private async Task<FeedView> Setup(Loader loader, int? placeholderCount = null)
	{
		var sut = (FeedView)XamlReader.Load(
			"""
			<mvux:FeedView
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:mvux="using:Uno.Extensions.Reactive.UI">
				<mvux:FeedView.ValueTemplate>
					<DataTemplate>
						<ListView ItemsSource="{Binding Data}">
							<ListView.ItemTemplate>
								<DataTemplate>
									<TextBlock Text="{Binding}" />
								</DataTemplate>
							</ListView.ItemTemplate>
						</ListView>
					</DataTemplate>
				</mvux:FeedView.ValueTemplate>
			</mvux:FeedView>
			""");

		// The SkeletonView templates come from ToolkitResources and the keyed style from the FeedView dictionary,
		// as an app would merge them.
		var resources = new ResourceDictionary();
		resources.MergedDictionaries.Add(new ToolkitResources());
		resources.MergedDictionaries.Add(new ResourceDictionary { Source = FeedViewResources });

		sut.Style = (Style)resources[SkeletonFeedViewStyle];
		if (placeholderCount is { } count)
		{
			Skeleton.SetPlaceholderCount(sut, count);
		}
		sut.Source = loader.Feed;

		await UIHelper.Load(new Grid { Resources = resources, Children = { sut } }, CT);

		return sut;
	}

	private static T GetPart<T>(FeedView feedView, string name)
		where T : FrameworkElement
		=> UIHelper.GetChildren<T>(feedView).Single(part => part.Name == name);

	private static ListView? GetListView(FrameworkElement presenter)
		=> UIHelper.GetChildren<ListView>(presenter).FirstOrDefault();

	/// <summary>
	/// A feed whose loads (initial and refreshes) wait until the test <see cref="Complete"/>s them.
	/// </summary>
	private sealed class Loader
	{
		private TaskCompletionSource<string[]> _pending = new();

		// Feed.Async caches feeds per delegate: capturing this instance gives each Loader its own feed.
		public Loader() => Feed = Reactive.Feed.Async(async ct => await _pending.Task);

		public IFeed<string[]> Feed { get; }

		/// <summary>Makes the loads started from now on (e.g. a refresh) wait for the next <see cref="Complete"/>.</summary>
		public void Reset() => _pending = new();

		/// <summary>Completes the pending loads with the given items.</summary>
		public void Complete(string[] items) => _pending.SetResult(items);
	}
}
