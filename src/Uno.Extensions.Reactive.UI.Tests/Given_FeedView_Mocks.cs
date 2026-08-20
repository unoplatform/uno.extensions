using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.Extensions.Reactive.Mocks;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.UI;
using Uno.Toolkit;
using Uno.UI.RuntimeTests;

[assembly: Uno.Extensions.Reactive.Config.GenerateModelMocks("Given_FeedView_Mocks")]

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsOnUIThread]
public partial class Given_FeedView_Mocks : FeedTests
{
	public partial class MockedPageModel
	{
		public IListFeed<string> Items => ListFeed.Async(async ct => ImmutableList.Create("real") as IImmutableList<string>);
	}
	[TestMethod]
	public async Task When_MockUndefined_Then_DataAxisReportedUndefined()
	{
		var sut = new FeedView { Source = MockFeed.Undefined<int>() };

		await UIHelper.Load(sut, CT);

		// The mocked message must flag the data axis as changed even though its value stays Undefined
		// (spec 012 §10.2), so the state (and the visual state selector) does observe the axis.
		await UIHelper.WaitFor(() => sut.State["Data"] is Option<object> { Type: OptionType.Undefined }, CT);
	}

	[TestMethod]
	public async Task When_MockLoading_Then_IsExecutingStaysTrue()
	{
		var sut = new FeedView { Source = MockFeed.Loading<int>() };
		var sutAsLoadable = (ILoadable)sut;

		await UIHelper.Load(sut, CT);

		await UIHelper.WaitFor(() => sut.State.Progress, CT);
		sutAsLoadable.IsExecuting.Should().BeTrue("a mocked loading feed completes while transient, pinning the FeedView in its loading state");
	}

	[TestMethod]
	public async Task When_MockLoading_Then_PinnedStateSurvivesReparenting()
	{
		// Models the Hot Design preview-group activation path (uno.hotdesign, specs/previews
		// "MVUX State Previews"): the FeedView subscribes once in an off-screen host, unloads,
		// then is re-parented — a second subscription to the same, already-completed mock feed.
		var feed = MockFeed.Loading<int>();
		var sut = new FeedView { Source = feed };
		var sutAsLoadable = (ILoadable)sut;

		var firstHost = new Grid { Children = { sut } };
		await UIHelper.Load(firstHost, CT);
		await UIHelper.WaitFor(() => sut.State.Progress, CT);

		firstHost.Children.Remove(sut);
		await UIHelper.WaitForIdle(CT);

		var secondHost = new Grid { Children = { sut } };
		await UIHelper.Load(secondHost, CT);

		// Settle so a wrongly-cleared transient state would have flipped the flags by now.
		await UIHelper.WaitForIdle(CT);
		await UIHelper.WaitForIdle(CT);

		sut.State.Progress.Should().BeTrue("the pinned loading state must survive re-parenting");
		sutAsLoadable.IsExecuting.Should().BeTrue("a second subscription to the completed mock must replay the transient state, not clear it");
	}

	[TestMethod]
	public async Task When_MockEmpty_Then_DataAxisReportedNone()
	{
		var sut = new FeedView { Source = MockFeed.Empty<int>() };

		await UIHelper.Load(sut, CT);

		await UIHelper.WaitFor(() => sut.State["Data"] is Option<object> { Type: OptionType.None }, CT);
	}

	[TestMethod]
	public async Task When_MockValue_Then_StateDataIsValue()
	{
		var sut = new FeedView { Source = MockFeed.Value(42) };

		await UIHelper.Load(sut, CT);

		await UIHelper.WaitFor(() => Equals(sut.State.Data, 42), CT);
		sut.State.Error.Should().BeNull();
		sut.State.Progress.Should().BeFalse();
	}

	[TestMethod]
	public async Task When_MockError_Then_StateErrorIsSet()
	{
		var error = new TestException();
		var sut = new FeedView { Source = MockFeed.Error<int>(error) };

		await UIHelper.Load(sut, CT);

		await UIHelper.WaitFor(() => sut.State.Error == error, CT);
	}

	[TestMethod]
	public async Task When_MockRefreshing_Then_StaleDataWithProgress()
	{
		var sut = new FeedView { Source = MockFeed.Refreshing(42) };

		await UIHelper.Load(sut, CT);

		await UIHelper.WaitFor(() => Equals(sut.State.Data, 42) && sut.State.Progress, CT);
	}

	[TestMethod]
	public async Task When_MockListFeedEmptyList_Then_DataIsEmptyList()
	{
		var sut = new FeedView { Source = MockListFeed.EmptyList<int>() };

		await UIHelper.Load(sut, CT);

		// Some(empty list) — distinct from the None produced by MockListFeed.Empty.
		await UIHelper.WaitFor(() => sut.State["Data"] is Option<object> { Type: OptionType.Some }, CT);
	}

	[TestMethod]
	public async Task When_CreateMockUnconfigured_Then_FeedViewSettlesInUndefinedPresentation()
	{
		// End-to-end through the generated VM: the mock feed is wrapped in a state, whose replay to
		// late subscribers recomputes change sets by value. Whatever the subscription timing, the
		// FeedView must settle in the undefined presentation: no data, no error, not loading.
		await using var vm = MockedPageViewModel.CreateMock();
		var sut = new FeedView { Source = vm.Items };
		var sutAsLoadable = (ILoadable)sut;

		await UIHelper.Load(sut, CT);

		await UIHelper.WaitFor(() => !sutAsLoadable.IsExecuting, CT);
		sut.State.Data.Should().BeNull();
		sut.State.Error.Should().BeNull();
		sut.State.Progress.Should().BeFalse();
	}

	[TestMethod]
	public async Task When_CreateMockWithValue_Then_FeedViewObservesValue()
	{
		await using var vm = MockedPageViewModel.CreateMock(m => m.Items = MockListFeed.Value("a", "b"));
		var sut = new FeedView { Source = vm.Items };

		await UIHelper.Load(sut, CT);

		await UIHelper.WaitFor(() => sut.State["Data"] is Option<object> { Type: OptionType.Some }, CT);
	}
}
