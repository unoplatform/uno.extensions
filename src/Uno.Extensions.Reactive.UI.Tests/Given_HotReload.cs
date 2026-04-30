#if DEBUG // Hot-reload tests are only relevant in debug configuration
using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsInSecondaryApp(ignoreIfNotSupported: true)]
public class Given_HotReload
{
	[TestInitialize]
	public void Setup()
	{
		// Allow more time for the dev-server to load the Roslyn workspace (solution can be large)
		HotReloadHelper.DefaultWorkspaceTimeout = TimeSpan.FromSeconds(300);
		// Allow more time for the first metadata update (delta compilation can be slow on CI)
		HotReloadHelper.DefaultMetadataUpdateTimeout = TimeSpan.FromSeconds(60);
	}

	[TestMethod]
	public async Task When_UpdateMethodBody_Then_MetadataUpdateReceived(CancellationToken ct)
	{
		Assert.AreEqual("original", HotReloadTarget.GetValue());

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/HotReloadTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		Assert.AreEqual("updated", HotReloadTarget.GetValue());
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_UpdateMvuxFeedSource_Then_NewViewModelReflectsUpdate(CancellationToken ct)
	{
		await using var vm = new MvuxHotReloadViewModel();
		var text = new TextBlock();
		var ui = new StackPanel { DataContext = vm, Children = { text } };
		text.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentValue") });

		await UIHelper.Load(ui, ct);
		await TestHelper.WaitFor(() => text.Text == "original", ct);

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// New ViewModel instance picks up the HR'd method body
		await using var vm2 = new MvuxHotReloadViewModel();
		var text2 = new TextBlock();
		var ui2 = new StackPanel { DataContext = vm2, Children = { text2 } };
		text2.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentValue") });

		await UIHelper.Load(ui2, ct);
		await TestHelper.WaitFor(() => text2.Text == "updated", ct);

		Assert.AreEqual("updated", text2.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RemoveAndReAddFeedProperty_Then_BindingsWork(CancellationToken ct)
	{
		await using var vm1 = new MvuxHotReloadFeedRemoveViewModel();
		var text1 = new TextBlock();
		var ui1 = new StackPanel { DataContext = vm1, Children = { text1 } };
		text1.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentValue") });

		await UIHelper.Load(ui1, ct);
		await TestHelper.WaitFor(() => text1.Text == "hello", ct);
		Assert.AreEqual("hello", text1.Text);

		// HR: Remove the Feed property
		await using var delta1 = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadFeedRemoveModel.cs",
			"""public IFeed<string> CurrentValue => Feed.Async(async ct => "hello");""",
			"""// property removed""",
			ct);

		// HR: Re-add the Feed property
		await using var delta2 = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadFeedRemoveModel.cs",
			"""// property removed""",
			"""public IFeed<string> CurrentValue => Feed.Async(async ct => "hello");""",
			ct);

		// New ViewModel after re-add should have working bindings
		await using var vm2 = new MvuxHotReloadFeedRemoveViewModel();
		var text2 = new TextBlock();
		var ui2 = new StackPanel { DataContext = vm2, Children = { text2 } };
		text2.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentValue") });

		await UIHelper.Load(ui2, ct);
		await TestHelper.WaitFor(() => text2.Text == "hello", ct);
		Assert.AreEqual("hello", text2.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RemoveAndReAddListFeedProperty_Then_BindingsWork(CancellationToken ct)
	{
		await using var vm1 = new MvuxHotReloadListFeedRemoveViewModel();
		var list1 = new ListView();
		list1.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });
		var ui1 = new StackPanel { DataContext = vm1, Children = { list1 } };

		await UIHelper.Load(ui1, ct);
		await TestHelper.WaitFor(() => list1.Items.Count == 3, ct);
		Assert.AreEqual(3, list1.Items.Count);

		// HR: Remove the ListFeed property
		await using var delta1 = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadListFeedRemoveModel.cs",
			"""public IListFeed<string> Items => ListFeed.Async(async ct => ImmutableList.Create("one", "two", "three"));""",
			"""// property removed""",
			ct);

		// HR: Re-add the ListFeed property
		await using var delta2 = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadListFeedRemoveModel.cs",
			"""// property removed""",
			"""public IListFeed<string> Items => ListFeed.Async(async ct => ImmutableList.Create("one", "two", "three"));""",
			ct);

		// New ViewModel after re-add should have working bindings
		await using var vm2 = new MvuxHotReloadListFeedRemoveViewModel();
		var list2 = new ListView();
		list2.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });
		var ui2 = new StackPanel { DataContext = vm2, Children = { list2 } };

		await UIHelper.Load(ui2, ct);
		await TestHelper.WaitFor(() => list2.Items.Count == 3, ct);
		Assert.AreEqual(3, list2.Items.Count);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RemoveAndReAddStateProperty_Then_BindingsWork(CancellationToken ct)
	{
		await using var vm1 = new MvuxHotReloadStateRemoveViewModel();
		var text1 = new TextBlock();
		var ui1 = new StackPanel { DataContext = vm1, Children = { text1 } };
		text1.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentValue") });

		await UIHelper.Load(ui1, ct);
		await TestHelper.WaitFor(() => text1.Text == "stateful", ct);
		Assert.AreEqual("stateful", text1.Text);

		await using var delta1 = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadStateRemoveModel.cs",
			"""public IState<string> CurrentValue => State.Async(this, async ct => "stateful");""",
			"""// property removed""",
			ct);

		await using var delta2 = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadStateRemoveModel.cs",
			"""// property removed""",
			"""public IState<string> CurrentValue => State.Async(this, async ct => "stateful");""",
			ct);

		await using var vm2 = new MvuxHotReloadStateRemoveViewModel();
		var text2 = new TextBlock();
		var ui2 = new StackPanel { DataContext = vm2, Children = { text2 } };
		text2.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentValue") });

		await UIHelper.Load(ui2, ct);
		await TestHelper.WaitFor(() => text2.Text == "stateful", ct);
		Assert.AreEqual("stateful", text2.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RemoveAndReAddMultipleProperties_Then_AllBindingsWork(CancellationToken ct)
	{
		await using var vm1 = new MvuxHotReloadMultiViewModel();
		var text1 = new TextBlock();
		text1.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Title") });
		var list1 = new ListView();
		list1.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });
		var ui1 = new StackPanel { DataContext = vm1, Children = { text1, list1 } };

		await UIHelper.Load(ui1, ct);
		await TestHelper.WaitFor(() => text1.Text == "title" && list1.Items.Count == 3, ct);

		await using var delta1a = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadMultiModel.cs",
			"""public IFeed<string> Title => Feed.Async(async ct => "title");""",
			"""// Title removed""",
			ct);

		await using var delta1b = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadMultiModel.cs",
			"""public IListFeed<string> Items => ListFeed.Async(async ct => ImmutableList.Create("a", "b", "c"));""",
			"""// Items removed""",
			ct);

		await using var delta2a = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadMultiModel.cs",
			"""// Title removed""",
			"""public IFeed<string> Title => Feed.Async(async ct => "title");""",
			ct);

		await using var delta2b = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadMultiModel.cs",
			"""// Items removed""",
			"""public IListFeed<string> Items => ListFeed.Async(async ct => ImmutableList.Create("a", "b", "c"));""",
			ct);

		await using var vm2 = new MvuxHotReloadMultiViewModel();
		var text2 = new TextBlock();
		text2.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Title") });
		var list2 = new ListView();
		list2.SetBinding(ItemsControl.ItemsSourceProperty, new Binding { Path = new PropertyPath("Items") });
		var ui2 = new StackPanel { DataContext = vm2, Children = { text2, list2 } };

		await UIHelper.Load(ui2, ct);
		await TestHelper.WaitFor(() => text2.Text == "title" && list2.Items.Count == 3, ct);
		Assert.AreEqual("title", text2.Text);
		Assert.AreEqual(3, list2.Items.Count);
	}
}
#endif
