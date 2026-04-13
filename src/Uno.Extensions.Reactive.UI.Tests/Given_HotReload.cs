#if DEBUG // Hot-reload tests are only relevant in debug configuration
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Config;
using Uno.Extensions.Reactive.Testing;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsInSecondaryApp(ignoreIfNotSupported: true)]
public partial class Given_HotReload : FeedTests
{
	[TestInitialize]
	public override void Initialize()
	{
		FeedConfiguration.HotReload = HotReloadSupport.Enabled;
		typeof(FeedConfiguration).GetField("_effectiveHotReload", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, null);

		// Allow more time for the dev-server to load the Roslyn workspace (solution can be large)
		HotReloadHelper.DefaultWorkspaceTimeout = TimeSpan.FromSeconds(300);
		// Allow more time for the first metadata update (delta compilation can be slow on CI)
		HotReloadHelper.DefaultMetadataUpdateTimeout = TimeSpan.FromSeconds(60);

		base.Initialize();
	}

	[TestCleanup]
	public override void Cleanup()
	{
		FeedConfiguration.HotReload = null;
		typeof(FeedConfiguration).GetField("_effectiveHotReload", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, null);

		base.Cleanup();
	}

	[TestMethod]
	public async Task When_UpdateFeedMessage_Then_BoundTextBlockUpdated()
	{
		var vm = new HotReloadTargetViewModel();
		var textBlock = new TextBlock();
		textBlock.DataContext = vm;
		textBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Message") });

		await UIHelper.Load(textBlock, CT);
		await TestHelper.WaitFor(() => textBlock.Text == "Initial message", CT);

		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/HotReloadTarget.cs",
			"""Feed.Async(async ct => "Initial message")""",
			"""Feed.Async(async ct => "Updated message")""",
			CT);

		await TestHelper.WaitFor(() => textBlock.Text == "Updated message", CT);
		Assert.AreEqual("Updated message", textBlock.Text);
	}

	[TestMethod]
	public async Task When_UpdateMultipleFeedBindings_Then_AllBoundTextBlocksUpdated()
	{
		var vm = new HotReloadTargetMultipleFeedsViewModel();

		var firstBlock = new TextBlock();
		firstBlock.DataContext = vm;
		firstBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("FirstFeed") });

		var secondBlock = new TextBlock();
		secondBlock.DataContext = vm;
		secondBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("SecondFeed") });

		var root = new StackPanel { Children = { firstBlock, secondBlock } };
		await UIHelper.Load(root, CT);

		await TestHelper.WaitFor(() => firstBlock.Text == "First original", CT);
		await TestHelper.WaitFor(() => secondBlock.Text == "Second original", CT);

		await using var update1 = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/HotReloadTarget.cs",
			""""First original"""",
			""""First updated"""",
			CT);

		await TestHelper.WaitFor(() => firstBlock.Text == "First updated", CT);
		Assert.AreEqual("First updated", firstBlock.Text);

		await using var update2 = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/HotReloadTarget.cs",
			""""Second original"""",
			""""Second updated"""",
			CT);

		await TestHelper.WaitFor(() => secondBlock.Text == "Second updated", CT);
		Assert.AreEqual("Second updated", secondBlock.Text);
	}

	[TestMethod]
	public async Task When_ChangeFeedType_Then_BoundTextBlockUpdated()
	{
		var vm = new HotReloadTargetViewModel();
		var textBlock = new TextBlock();
		textBlock.DataContext = vm;
		textBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("Message") });

		await UIHelper.Load(textBlock, CT);
		await TestHelper.WaitFor(() => textBlock.Text == "Initial message", CT);

		// Change from Feed.Async to Feed.Dynamic
		await using var _ = await HotReloadHelper.UpdateSourceFile(
			"../../Uno.Extensions.Reactive.UI.Tests/HotReloadTarget.cs",
			"""public IFeed<string> Message => Feed.Async(async ct => "Initial message");""",
			"""public IFeed<string> Message => Feed.Dynamic(async ct => "Changed to Dynamic");""",
			CT);

		await TestHelper.WaitFor(() => textBlock.Text == "Changed to Dynamic", CT);
		Assert.AreEqual("Changed to Dynamic", textBlock.Text);
	}
}
#endif
