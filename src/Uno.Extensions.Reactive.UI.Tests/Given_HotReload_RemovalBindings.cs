using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
public partial class Given_HotReload_RemovalBindings : FeedTests
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

	#region When_RemoveFeed_WithKeepPrevious_Then_BoundTextBlockRetainsValue
	[TestMethod]
	public async Task When_RemoveFeed_WithKeepPrevious_Then_BoundTextBlockRetainsValue()
	{
		FeedConfiguration.HotReloadRemovalBehavior = HotReloadRemovalBehavior.KeepPrevious;

		var vm = new When_RemoveFeed_WithKeepPrevious_Then_BoundTextBlockRetainsValue_MyViewModel();

		var firstBlock = new TextBlock();
		firstBlock.DataContext = vm;
		firstBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyFeed") });

		var secondBlock = new TextBlock();
		secondBlock.DataContext = vm;
		secondBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MySecondFeed") });

		var root = new StackPanel { Children = { firstBlock, secondBlock } };
		await UIHelper.Load(root, CT);

		await TestHelper.WaitFor(() => firstBlock.Text == "First feed original", CT);
		await TestHelper.WaitFor(() => secondBlock.Text == "Second feed original", CT);

		// v1 removes MySecondFeed
		HotReloadService.UpdateApplication(new[] { typeof(When_RemoveFeed_WithKeepPrevious_Then_BoundTextBlockRetainsValue_v1) });

		await TestHelper.WaitFor(() => firstBlock.Text == "First feed updated", CT);
		// With KeepPrevious, the removed feed's TextBlock should retain its last value
		Assert.AreEqual("Second feed original", secondBlock.Text);
	}

	[ReactiveBindable(true)]
	public partial class When_RemoveFeed_WithKeepPrevious_Then_BoundTextBlockRetainsValue_MyModel
	{
		public IFeed<string> MyFeed => Feed.Async(async ct => "First feed original");
		public IFeed<string> MySecondFeed => Feed.Async(async ct => "Second feed original");
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_RemoveFeed_WithKeepPrevious_Then_BoundTextBlockRetainsValue_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_RemoveFeed_WithKeepPrevious_Then_BoundTextBlockRetainsValue_MyModel))]
	public partial class When_RemoveFeed_WithKeepPrevious_Then_BoundTextBlockRetainsValue_v1 : IAsyncDisposable
	{
		internal When_RemoveFeed_WithKeepPrevious_Then_BoundTextBlockRetainsValue_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyFeed => Feed.Async(async ct => "First feed updated");
		// MySecondFeed intentionally removed

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion

	#region When_RemoveFeed_WithClear_Then_BoundTextBlockCleared
	[TestMethod]
	public async Task When_RemoveFeed_WithClear_Then_BoundTextBlockCleared()
	{
		FeedConfiguration.HotReloadRemovalBehavior = HotReloadRemovalBehavior.Clear;

		var vm = new When_RemoveFeed_WithClear_Then_BoundTextBlockCleared_MyViewModel();

		var firstBlock = new TextBlock();
		firstBlock.DataContext = vm;
		firstBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyFeed") });

		var secondBlock = new TextBlock();
		secondBlock.DataContext = vm;
		secondBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MySecondFeed") });

		var root = new StackPanel { Children = { firstBlock, secondBlock } };
		await UIHelper.Load(root, CT);

		await TestHelper.WaitFor(() => firstBlock.Text == "First feed original", CT);
		await TestHelper.WaitFor(() => secondBlock.Text == "Second feed original", CT);

		// v1 removes MySecondFeed
		HotReloadService.UpdateApplication(new[] { typeof(When_RemoveFeed_WithClear_Then_BoundTextBlockCleared_v1) });

		await TestHelper.WaitFor(() => firstBlock.Text == "First feed updated", CT);
		// With Clear, the removed feed's TextBlock should be cleared
		await TestHelper.WaitFor(() => string.IsNullOrEmpty(secondBlock.Text), CT);
	}

	[ReactiveBindable(true)]
	public partial class When_RemoveFeed_WithClear_Then_BoundTextBlockCleared_MyModel
	{
		public IFeed<string> MyFeed => Feed.Async(async ct => "First feed original");
		public IFeed<string> MySecondFeed => Feed.Async(async ct => "Second feed original");
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_RemoveFeed_WithClear_Then_BoundTextBlockCleared_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_RemoveFeed_WithClear_Then_BoundTextBlockCleared_MyModel))]
	public partial class When_RemoveFeed_WithClear_Then_BoundTextBlockCleared_v1 : IAsyncDisposable
	{
		internal When_RemoveFeed_WithClear_Then_BoundTextBlockCleared_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyFeed => Feed.Async(async ct => "First feed updated");
		// MySecondFeed intentionally removed

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion

	#region When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated
	[TestMethod]
	public async Task When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated()
	{
		FeedConfiguration.HotReloadRemovalBehavior = HotReloadRemovalBehavior.Clear;

		var vm = new When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated_MyViewModel();

		var firstBlock = new TextBlock();
		firstBlock.DataContext = vm;
		firstBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyFeed") });

		var secondBlock = new TextBlock();
		secondBlock.DataContext = vm;
		secondBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MySecondFeed") });

		var root = new StackPanel { Children = { firstBlock, secondBlock } };
		await UIHelper.Load(root, CT);

		await TestHelper.WaitFor(() => firstBlock.Text == "First feed original", CT);
		await TestHelper.WaitFor(() => secondBlock.Text == "Second feed original", CT);

		// v1: Remove MySecondFeed
		HotReloadService.UpdateApplication(new[] { typeof(When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated_v1) });

		await TestHelper.WaitFor(() => firstBlock.Text == "First feed v1", CT);
		await TestHelper.WaitFor(() => string.IsNullOrEmpty(secondBlock.Text), CT);

		// v2: Restore MySecondFeed with a new value
		HotReloadService.UpdateApplication(new[] { typeof(When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated_v2) });

		await TestHelper.WaitFor(() => firstBlock.Text == "First feed v2", CT);
		await TestHelper.WaitFor(() => secondBlock.Text == "Second feed restored", CT);
		Assert.AreEqual("Second feed restored", secondBlock.Text);
	}

	[ReactiveBindable(true)]
	public partial class When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated_MyModel
	{
		public IFeed<string> MyFeed => Feed.Async(async ct => "First feed original");
		public IFeed<string> MySecondFeed => Feed.Async(async ct => "Second feed original");
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated_MyModel))]
	public partial class When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated_v1 : IAsyncDisposable
	{
		internal When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyFeed => Feed.Async(async ct => "First feed v1");
		// MySecondFeed intentionally removed

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated_MyModel))]
	public partial class When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated_v2 : IAsyncDisposable
	{
		internal When_RemoveFeed_ThenRestore_Then_BoundTextBlockUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyFeed => Feed.Async(async ct => "First feed v2");
		public IFeed<string> MySecondFeed => Feed.Async(async ct => "Second feed restored");

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion
}
