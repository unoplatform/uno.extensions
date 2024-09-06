﻿#define __SKIA__

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Foundation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Testing;
using Uno.UI.RuntimeTests;
using ListView = Microsoft.UI.Xaml.Controls.ListView;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsOnUIThread]
public partial class Given_BindableCollection_Selection : FeedTests
{
	public partial class Given_BindableCollection_Selection_Model
	{
		public IListState<MyItem> Items => ListState.Value(this, () => ImmutableList.Create<MyItem>(new(41), new(42), new(43)));
	}

	public partial record MyItem([property:Key] int Value, int Version = 1);

	[TestMethod]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !__SKIA__
	[Ignore("Pointer injection not supported yet on this platform")]
#endif
	public async Task When_SelectSingleFromView_ListView()
	{
		var (vm, lv, items) = await SetupListView(ListViewSelectionMode.Single);

		InputInjectorHelper.Current.Tap(items[1]);

		await TestHelper.WaitFor(async ct => (await vm.Items.GetSelectedItem(ct))?.Value == 42, CT);
	}

	[TestMethod]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !__SKIA__
	[Ignore("Pointer injection not supported yet on this platform")]
#endif
	public async Task When_SelectMultipleFromView_ListView()
	{
		var (vm, lv, items) = await SetupListView(ListViewSelectionMode.Multiple);

		InputInjectorHelper.Current.Tap(items[0]);
		InputInjectorHelper.Current.Tap(items[1]);

		await TestHelper.WaitFor(async ct =>
		{
			return (await vm.Items.GetSelectedItems(ct)).SequenceEqual(new MyItem[] { new(41), new(42) });
		}, CT);
	}

	[TestMethod]
	public async Task When_PreselectedItem_SelectedItems_ListView()
	{
		var (vm, lv, items) = await SetupListView(ListViewSelectionMode.Multiple);

		items[0].IsSelected = true;

		await TestHelper.WaitFor(async ct =>
		{
			return (await vm.Items.GetSelectedItems(ct)).SequenceEqual(new MyItem[] { });
		}, CT);
	}

	[TestMethod]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !__SKIA__
	[Ignore("Pointer injection not supported yet on this platform")]
#endif
	public async Task When_EditSingleSelectedItem_Then_SelectionPreserved()
	{
		var (vm, lv, items) = await SetupListView(ListViewSelectionMode.Single);

		InputInjectorHelper.Current.Tap(items[1]);

		await TestHelper.WaitFor(async ct =>
		{
			// Selection is preserved on ...
			return lv.SelectedItem is MyItem { Value: 42 } // ... the ListView ...
				&& ((ISelectionInfo)lv.ItemsSource).IsSelected(1) // ... the BindableCollection ...
				&& await vm.Items.GetSelectedItem(ct) is { Value: 42 }; // ... and the Feed!
		}, CT);

		await vm.Model.Items.UpdateAsync(items => items.Replace(items[1], items[1] with { Version = 2 }), CT);

		await TestHelper.WaitFor(async ct =>
		{
			// Selection is preserved on ...
			return lv.SelectedItem is MyItem { Value: 42, Version: 2 } // ... the ListView ...
				&& ((ISelectionInfo)lv.ItemsSource).IsSelected(1) // ... the BindableCollection ...
				&& await vm.Items.GetSelectedItem(ct) is { Value: 42, Version: 2 }; // ... and the Feed!
		}, CT);
	}

	private async Task<(BindableGiven_BindableCollection_Selection_Model, ListView, ListViewItem[])> SetupListView(ListViewSelectionMode selectionMode)
	{
		var vm = new BindableGiven_BindableCollection_Selection_Model();
		var lv = new ListView { ItemsSource = vm.Items, SelectionMode = selectionMode };

		await UIHelper.Load(lv, CT);

		var lvItems = Array.Empty<ListViewItem>();
		await TestHelper.WaitFor(async ct =>
		{
			lvItems = UIHelper.GetChildren<ListViewItem>(lv).ToArray();
			return lvItems.Length > 0;
		}, CT);

		return (vm, lv, lvItems);
	}
}
