using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;
using Uno.UI.RuntimeTests;
using Microsoft.UI.Xaml.Data;
using ListView = Microsoft.UI.Xaml.Controls.ListView;


namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsOnUIThread]
public partial class Given_BindableCollection_Edition : FeedTests
{
	public partial class Given_BindableCollection_Edition_Model
	{
		public IListState<int> Items => ListState.Value(this, () => ImmutableList.Create(41, 42, 43));
	}

	[TestMethod]
	[InjectedPointer(PointerDeviceType.Touch)]
	public async Task When_EditFromView()
	{
		var vm = new BindableGiven_BindableCollection_Edition_Model();
		var collectionView = (await (vm.Items as ISignal<IMessage>).GetSource(Context, CT).FirstAsync()).Current.Data.SomeOrDefault() as ICollectionView;

		Assert.IsNotNull(collectionView);

		collectionView.RemoveAt(1);
		collectionView.Insert(0, 42);

		await UIHelper.WaitFor(async ct =>
		{
			var items = await vm.Items;
			var isRightOrder = items.SequenceEqual(new[] { 42, 41, 43 });

			return isRightOrder;
		}, CT);
	}

	private async Task<(BindableGiven_BindableCollection_Edition_Model, ListView, ListViewItem[])> SetupListView()
	{
		var vm = new BindableGiven_BindableCollection_Edition_Model();
		var lv = new ListView { ItemsSource = vm.Items, CanDragItems = true, AllowDrop = true, CanReorderItems = true };

		await UIHelper.Load(lv, CT);

		var lvItems = Array.Empty<ListViewItem>();
		await UIHelper.WaitFor(async ct =>
		{
			lvItems = UIHelper.GetChildren<ListViewItem>(lv).ToArray();
			return lvItems.Length > 0;
		}, CT);

		return (vm, lv, lvItems);
	}
}
