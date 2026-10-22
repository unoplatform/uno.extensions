using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.UI;
using Uno.UI.RuntimeTests;
using ListView = Microsoft.UI.Xaml.Controls.ListView;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsOnUIThread]
public partial class Given_FeedView_Selection : FeedTests
{
	private const int PageSize = 20;

	public partial class Given_FeedView_Selection_Model
	{
		private int _loads;

		public IListFeed<Item> Items => ListFeed
			.PaginatedAsync(async (PageRequest request, CancellationToken ct) => Load(request.CurrentCount))
			.Selection(Selected);

		public IState<Item> Selected => State<Item>.Empty(this);

		// A paginated list without the Selection operator: the selection is only kept by the list state created for the binding, as a volatile update.
		public IListFeed<Item> Plain => ListFeed.PaginatedAsync(async (PageRequest request, CancellationToken ct) => Load(request.CurrentCount));

		public IListState<Item> Editable => ListState
			.Async(this, async ct => Load(0))
			.Selection(EditableSelected);

		public IState<Item> EditableSelected => State<Item>.Empty(this);

		private IImmutableList<Item> Load(uint start)
		{
			// Each load of the first page (initial load or refresh) gets a new generation, so the test can tell the refreshed items apart.
			var generation = start is 0 ? Interlocked.Increment(ref _loads) : _loads;

			return Enumerable
				.Range((int)start, PageSize)
				.Select(i => new Item($"I-{i + 1:000}", $"Item {i + 1}", generation))
				.ToImmutableList();
		}
	}

	public partial record Item([property: Key] string Id, string Name, int Generation);

	[TestMethod]
	public async Task When_SelectedFromListView_And_FeedViewRefreshed_Then_SelectionKept()
	{
		// https://github.com/unoplatform/uno.extensions/issues/3258
		var (vm, feedView, lv) = await Setup(nameof(Given_FeedView_Selection_Model.Items));

		lv.SelectedIndex = 6;
		await WaitForSelection(vm, lv, "I-007", "after the selection made on the ListView");

		await Refresh(feedView, lv);

		lv.Should().BeSameAs(GetListView(feedView), "the FeedView should not have re-applied its template");
		await WaitForSelection(vm, lv, "I-007", "after the FeedView has been refreshed");
		lv.SelectedItem.Should().BeSameAs(lv.Items[6], "the ListView should select the refreshed instance of the item");

		await LoadNextPage(lv);
		await WaitForSelection(vm, lv, "I-007", "after the next page has been loaded");
	}

	[TestMethod]
	public async Task When_SelectedFromListView_And_NextPageLoaded_Then_SelectionKept()
	{
		var (vm, _, lv) = await Setup(nameof(Given_FeedView_Selection_Model.Items));

		lv.SelectedIndex = 6;
		await WaitForSelection(vm, lv, "I-007", "after the selection made on the ListView");

		await LoadNextPage(lv);
		await WaitForSelection(vm, lv, "I-007", "after the next page has been loaded");
	}

	[TestMethod]
	public async Task When_SelectedFromListView_And_ListFeedWithoutSelectionRefreshed_Then_ListViewMatchesState()
	{
		var (vm, feedView, lv) = await Setup(nameof(Given_FeedView_Selection_Model.Plain));

		lv.SelectedIndex = 6;
		await UIHelper.WaitFor(async ct => (await ((IListState<Item>)vm.Plain).GetSelectedItem(ct))?.Id == "I-007", CT);

		await Refresh(feedView, lv);

		// The selection might or might not be kept, but the ListView must reflect the list state.
		try
		{
			await UIHelper.WaitFor(async ct => (lv.SelectedItem as Item)?.Id == (await ((IListState<Item>)vm.Plain).GetSelectedItem(ct))?.Id, CT);
		}
		catch (TimeoutException)
		{
			var state = await ((IListState<Item>)vm.Plain).GetSelectedItem(CT);
			Assert.Fail($"ListView.SelectedItem={(lv.SelectedItem as Item)?.Id ?? "<none>"} while the list state selected item is {state?.Id ?? "<none>"}.");
		}
	}

	[TestMethod]
	public async Task When_SelectedFromListView_And_ItemAddedToListState_Then_SelectionKept()
	{
		var (vm, _, lv) = await Setup(nameof(Given_FeedView_Selection_Model.Editable));

		lv.SelectedIndex = 6;
		await UIHelper.WaitFor(async ct => (await vm.Model.EditableSelected.Value(ct))?.Id == "I-007", CT);

		await ((IListState<Item>)vm.Editable).AddAsync(new Item("I-999", "Item 999", 0), CT);
		await UIHelper.WaitFor(() => lv.Items.Count == PageSize + 1, CT);

		try
		{
			await UIHelper.WaitFor(async ct => (lv.SelectedItem as Item)?.Id == "I-007" && (await vm.Model.EditableSelected.Value(ct))?.Id == "I-007", CT);
		}
		catch (TimeoutException)
		{
			var state = await vm.Model.EditableSelected.Value(CT);
			Assert.Fail($"Selection is not 'I-007' after an item has been added: ListView.SelectedItem={(lv.SelectedItem as Item)?.Id ?? "<none>"}, state={state?.Id ?? "<none>"}.");
		}
	}

	private async Task<(Given_FeedView_Selection_ViewModel, FeedView, ListView)> Setup(string source)
	{
		var vm = new Given_FeedView_Selection_ViewModel();
		var feedView = (FeedView)XamlReader.Load(
			$$"""
			<mvux:FeedView
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:mvux="using:Uno.Extensions.Reactive.UI"
				Source="{Binding {{source}}}"
				RefreshingState="None">
				<mvux:FeedView.ValueTemplate>
					<DataTemplate>
						<ListView ItemsSource="{Binding Data}" SelectionMode="Single" />
					</DataTemplate>
				</mvux:FeedView.ValueTemplate>
			</mvux:FeedView>
			""");
		feedView.DataContext = vm;

		await UIHelper.Load(feedView, CT);

		var lv = default(ListView);
		await UIHelper.WaitFor(() => (lv = GetListView(feedView)) is { Items.Count: PageSize }, CT);

		return (vm, feedView, lv!);
	}

	private static ListView? GetListView(FeedView feedView)
		=> UIHelper.GetChildren<ListView>(feedView).FirstOrDefault();

	private async Task Refresh(FeedView feedView, ListView lv)
	{
		var generation = ((Item)lv.Items[6]).Generation;

		feedView.Refresh.Execute(null);
		await UIHelper.WaitFor(() => lv.Items.Count > 6 && lv.Items[6] is Item { Generation: var g } && g > generation, CT);
	}

	private async Task LoadNextPage(ListView lv)
	{
		var count = lv.Items.Count;
		await ((ISupportIncrementalLoading)lv.ItemsSource).LoadMoreItemsAsync(PageSize).AsTask(CT);
		await UIHelper.WaitFor(() => lv.Items.Count == count + PageSize, CT);
	}

	private async Task WaitForSelection(Given_FeedView_Selection_ViewModel vm, ListView lv, string expectedId, string step)
	{
		try
		{
			await UIHelper.WaitFor(async ct => (lv.SelectedItem as Item)?.Id == expectedId && (await vm.Model.Selected.Value(ct))?.Id == expectedId, CT);
		}
		catch (TimeoutException)
		{
			var state = await vm.Model.Selected.Value(CT);
			Assert.Fail($"Selection is not '{expectedId}' {step}: ListView.SelectedItem={(lv.SelectedItem as Item)?.Id ?? "<none>"}, state={state?.Id ?? "<none>"}.");
		}
	}
}
