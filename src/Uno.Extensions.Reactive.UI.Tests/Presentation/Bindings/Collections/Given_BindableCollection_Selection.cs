using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Input;
using Windows.Foundation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsOnUIThread]
public partial class Given_BindableCollection_Selection : FeedTests
{
	public partial class Given_BindableCollection_Selection_Model
	{
		public IListState<int> Items => ListState.Value(this, () => ImmutableList.Create(41, 42, 43));
	}

	[TestMethod]
	[InjectedPointer(PointerDeviceType.Mouse)]
#if !__SKIA__
	[Ignore("Pointer injection not supported yet on this platform")]
#endif
	public async Task When_SelectSingleFromView_ListView()
	{
		var (vm, lv, items) = await SetupListView(ListViewSelectionMode.Single);

		InputInjectorHelper.Current.Tap(items[1]);

		await UIHelper.WaitFor(async ct => await vm.Items.GetSelectedItem(ct) == 42, CT);
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

		await UIHelper.WaitFor(async ct =>
		{
			return (await vm.Items.GetSelectedItems(ct)).SequenceEqual(new[] { 41, 42 });
		}, CT);
	}

	private async Task<(BindableGiven_BindableCollection_Selection_Model, ListView, ListViewItem[])> SetupListView(ListViewSelectionMode selectionMode)
	{
		var vm = new BindableGiven_BindableCollection_Selection_Model();
		var lv = new ListView { ItemsSource = vm.Items, SelectionMode = selectionMode };

		await UIHelper.Load(lv, CT);

		var lvItems = Array.Empty<ListViewItem>();
		await UIHelper.WaitFor(async ct =>
		{
			lvItems = UIHelper.FindChildren<ListViewItem>(lv).ToArray();
			return lvItems.Length > 0;
		}, CT);

		return (vm, lv, lvItems);
	}
}
