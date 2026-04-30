#if DEBUG // Hot-reload tests are only relevant in debug configuration
using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.UI;
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
		var vm = new MvuxHotReloadViewModel();
		var text = new TextBlock();
		var ui = new StackPanel { DataContext = vm, Children = { text } };
		text.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentValue") });

		await UIHelper.Load(ui, default);
		await TestHelper.WaitFor(() => text.Text == "original", default);

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/MvuxHotReloadTarget.cs",
			"""return "original";""",
			"""return "updated";""",
			ct);

		// New ViewModel instance picks up the HR'd method body
		var vm2 = new MvuxHotReloadViewModel();
		var text2 = new TextBlock();
		var ui2 = new StackPanel { DataContext = vm2, Children = { text2 } };
		text2.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentValue") });

		await UIHelper.Load(ui2, default);
		await TestHelper.WaitFor(() => text2.Text == "updated", default);

		Assert.AreEqual("updated", text2.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RemoveAndReAddFeedProperty_Then_BindingsWork(CancellationToken ct)
	{
		var vm1 = new MvuxHotReloadFeedRemoveViewModel();
		var text1 = new TextBlock();
		var ui1 = new StackPanel { DataContext = vm1, Children = { text1 } };
		text1.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentValue") });

		await UIHelper.Load(ui1, default);
		await TestHelper.WaitFor(() => text1.Text == "hello", default);
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
		var vm2 = new MvuxHotReloadFeedRemoveViewModel();
		var text2 = new TextBlock();
		var ui2 = new StackPanel { DataContext = vm2, Children = { text2 } };
		text2.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("CurrentValue") });

		await UIHelper.Load(ui2, default);
		await TestHelper.WaitFor(() => text2.Text == "hello", default);
		Assert.AreEqual("hello", text2.Text);
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RemoveAndReAddListFeedProperty_Then_BindingsWork(CancellationToken ct)
	{
		var vm1 = new MvuxHotReloadListFeedRemoveViewModel();
		var list1 = new ListView { ItemsSource = vm1.Items };
		var ui1 = new StackPanel { DataContext = vm1, Children = { list1 } };

		await UIHelper.Load(ui1, default);
		await TestHelper.WaitFor(() => list1.Items.Count == 3, default);
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
		var vm2 = new MvuxHotReloadListFeedRemoveViewModel();
		var list2 = new ListView { ItemsSource = vm2.Items };
		var ui2 = new StackPanel { DataContext = vm2, Children = { list2 } };

		await UIHelper.Load(ui2, default);
		await TestHelper.WaitFor(() => list2.Items.Count == 3, default);
		Assert.AreEqual(3, list2.Items.Count);
	}
}
#endif
