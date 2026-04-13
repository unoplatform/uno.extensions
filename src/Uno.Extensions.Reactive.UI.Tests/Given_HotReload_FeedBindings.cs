using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Config;
using Uno.Extensions.Reactive.Core.HotReload;
using Uno.Extensions.Reactive.Testing;
using Uno.UI.RuntimeTests;

namespace Uno.Extensions.Reactive.WinUI.Tests;

[TestClass]
[RunsOnUIThread]
public partial class Given_HotReload_FeedBindings : FeedTests
{
	[TestInitialize]
	public override void Initialize()
	{
		FeedConfiguration.HotReload = HotReloadSupport.Enabled;
		typeof(FeedConfiguration).GetField("_effectiveHotReload", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, null);

		base.Initialize();
	}

	[TestCleanup]
	public override void Cleanup()
	{
		FeedConfiguration.HotReload = null;
		typeof(FeedConfiguration).GetField("_effectiveHotReload", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, null);
		FeedConfiguration.HotReloadRemovalBehavior = HotReloadRemovalBehavior.KeepPrevious;

		base.Cleanup();
	}

	#region When_UpdateFeed_Then_BoundTextBlockUpdated
	[TestMethod]
	public async Task When_UpdateFeed_Then_BoundTextBlockUpdated()
	{
		var vm = new When_UpdateFeed_Then_BoundTextBlockUpdated_MyViewModel();
		var textBlock = new TextBlock();
		textBlock.DataContext = vm;
		textBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyFeed") });

		await UIHelper.Load(textBlock, CT);
		await TestHelper.WaitFor(() => textBlock.Text == "Feed value from original model", CT);

		HotReloadService.UpdateApplication(new[] { typeof(When_UpdateFeed_Then_BoundTextBlockUpdated_MyModel_v1) });

		await TestHelper.WaitFor(() => textBlock.Text == "Feed value from model v1", CT);
		Assert.AreEqual("Feed value from model v1", textBlock.Text);
	}

	[ReactiveBindable(true)]
	public partial class When_UpdateFeed_Then_BoundTextBlockUpdated_MyModel
	{
		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from original model");
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_UpdateFeed_Then_BoundTextBlockUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_UpdateFeed_Then_BoundTextBlockUpdated_MyModel))]
	public partial class When_UpdateFeed_Then_BoundTextBlockUpdated_MyModel_v1 : IAsyncDisposable
	{
		internal When_UpdateFeed_Then_BoundTextBlockUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from model v1");

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion

	#region When_UpdateRecordFeed_Then_BoundTextBlocksUpdated
	[TestMethod]
	public async Task When_UpdateRecordFeed_Then_BoundTextBlocksUpdated()
	{
		var vm = new When_UpdateRecordFeed_Then_BoundTextBlocksUpdated_MyViewModel();

		var nameBlock = new TextBlock();
		nameBlock.DataContext = vm;
		nameBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyFeed.Name") });

		var valueBlock = new TextBlock();
		valueBlock.DataContext = vm;
		valueBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyFeed.Value") });

		var root = new StackPanel { Children = { nameBlock, valueBlock } };
		await UIHelper.Load(root, CT);

		await TestHelper.WaitFor(() => nameBlock.Text == "OriginalName", CT);
		await TestHelper.WaitFor(() => valueBlock.Text == "OriginalValue", CT);

		HotReloadService.UpdateApplication(new[] { typeof(When_UpdateRecordFeed_Then_BoundTextBlocksUpdated_MyModel_v1) });

		await TestHelper.WaitFor(() => nameBlock.Text == "UpdatedName", CT);
		await TestHelper.WaitFor(() => valueBlock.Text == "UpdatedValue", CT);
		Assert.AreEqual("UpdatedName", nameBlock.Text);
		Assert.AreEqual("UpdatedValue", valueBlock.Text);
	}

	public record When_UpdateRecordFeed_Then_BoundTextBlocksUpdated_Record(string Name, string Value);

	[ReactiveBindable(true)]
	public partial class When_UpdateRecordFeed_Then_BoundTextBlocksUpdated_MyModel
	{
		public IFeed<When_UpdateRecordFeed_Then_BoundTextBlocksUpdated_Record> MyFeed
			=> Feed.Async(async ct => new When_UpdateRecordFeed_Then_BoundTextBlocksUpdated_Record("OriginalName", "OriginalValue"));
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_UpdateRecordFeed_Then_BoundTextBlocksUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_UpdateRecordFeed_Then_BoundTextBlocksUpdated_MyModel))]
	public partial class When_UpdateRecordFeed_Then_BoundTextBlocksUpdated_MyModel_v1 : IAsyncDisposable
	{
		internal When_UpdateRecordFeed_Then_BoundTextBlocksUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<When_UpdateRecordFeed_Then_BoundTextBlocksUpdated_Record> MyFeed
			=> Feed.Async(async ct => new When_UpdateRecordFeed_Then_BoundTextBlocksUpdated_Record("UpdatedName", "UpdatedValue"));

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion

	#region When_UpdateMultipleFeeds_Then_AllBoundTextBlocksUpdated
	[TestMethod]
	public async Task When_UpdateMultipleFeeds_Then_AllBoundTextBlocksUpdated()
	{
		var vm = new When_UpdateMultipleFeeds_Then_AllBoundTextBlocksUpdated_MyViewModel();

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

		HotReloadService.UpdateApplication(new[] { typeof(When_UpdateMultipleFeeds_Then_AllBoundTextBlocksUpdated_MyModel_v1) });

		await TestHelper.WaitFor(() => firstBlock.Text == "First updated", CT);
		await TestHelper.WaitFor(() => secondBlock.Text == "Second updated", CT);
		Assert.AreEqual("First updated", firstBlock.Text);
		Assert.AreEqual("Second updated", secondBlock.Text);
	}

	[ReactiveBindable(true)]
	public partial class When_UpdateMultipleFeeds_Then_AllBoundTextBlocksUpdated_MyModel
	{
		public IFeed<string> FirstFeed => Feed.Async(async ct => "First original");
		public IFeed<string> SecondFeed => Feed.Async(async ct => "Second original");
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_UpdateMultipleFeeds_Then_AllBoundTextBlocksUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_UpdateMultipleFeeds_Then_AllBoundTextBlocksUpdated_MyModel))]
	public partial class When_UpdateMultipleFeeds_Then_AllBoundTextBlocksUpdated_MyModel_v1 : IAsyncDisposable
	{
		internal When_UpdateMultipleFeeds_Then_AllBoundTextBlocksUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> FirstFeed => Feed.Async(async ct => "First updated");
		public IFeed<string> SecondFeed => Feed.Async(async ct => "Second updated");

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion

	#region When_ChangeKindOfFeed_Then_BoundTextBlockUpdated
	[TestMethod]
	public async Task When_ChangeKindOfFeed_Then_BoundTextBlockUpdated()
	{
		var vm = new When_ChangeKindOfFeed_Then_BoundTextBlockUpdated_MyViewModel();
		var textBlock = new TextBlock();
		textBlock.DataContext = vm;
		textBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyFeed") });

		await UIHelper.Load(textBlock, CT);
		await TestHelper.WaitFor(() => textBlock.Text == "Feed value from original model", CT);

		// Change from Feed.Async to Feed.Dynamic
		HotReloadService.UpdateApplication(new[] { typeof(When_ChangeKindOfFeed_Then_BoundTextBlockUpdated_MyModel_v1) });

		await TestHelper.WaitFor(() => textBlock.Text == "Feed value from model v1", CT);
		Assert.AreEqual("Feed value from model v1", textBlock.Text);
	}

	[ReactiveBindable(true)]
	public partial class When_ChangeKindOfFeed_Then_BoundTextBlockUpdated_MyModel
	{
		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from original model");
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_ChangeKindOfFeed_Then_BoundTextBlockUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_ChangeKindOfFeed_Then_BoundTextBlockUpdated_MyModel))]
	public partial class When_ChangeKindOfFeed_Then_BoundTextBlockUpdated_MyModel_v1 : IAsyncDisposable
	{
		internal When_ChangeKindOfFeed_Then_BoundTextBlockUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyFeed => Feed.Dynamic(async ct => "Feed value from model v1");

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion
}
