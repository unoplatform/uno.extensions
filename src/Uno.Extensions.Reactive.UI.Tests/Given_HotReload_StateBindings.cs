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
public partial class Given_HotReload_StateBindings : FeedTests
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

	#region When_UpdateState_Then_BoundTextBlockUpdated
	[TestMethod]
	public async Task When_UpdateState_Then_BoundTextBlockUpdated()
	{
		var vm = new When_UpdateState_Then_BoundTextBlockUpdated_MyViewModel();
		var textBlock = new TextBlock();
		textBlock.DataContext = vm;
		textBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyState") });

		await UIHelper.Load(textBlock, CT);
		await TestHelper.WaitFor(() => textBlock.Text == "State value from original model", CT);

		HotReloadService.UpdateApplication(new[] { typeof(When_UpdateState_Then_BoundTextBlockUpdated_MyModel_v1) });

		await TestHelper.WaitFor(() => textBlock.Text == "State value from model v1", CT);
		Assert.AreEqual("State value from model v1", textBlock.Text);
	}

	[ReactiveBindable(true)]
	public partial class When_UpdateState_Then_BoundTextBlockUpdated_MyModel
	{
		public IState<string> MyState => State<string>.Value(this, () => "State value from original model");
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_UpdateState_Then_BoundTextBlockUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_UpdateState_Then_BoundTextBlockUpdated_MyModel))]
	public partial class When_UpdateState_Then_BoundTextBlockUpdated_MyModel_v1 : IAsyncDisposable
	{
		internal When_UpdateState_Then_BoundTextBlockUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyState => Feed.Async(async ct => "State value from model v1");

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion

	#region When_UpdateRecordState_Then_BoundTextBlocksUpdated
	[TestMethod]
	public async Task When_UpdateRecordState_Then_BoundTextBlocksUpdated()
	{
		var vm = new When_UpdateRecordState_Then_BoundTextBlocksUpdated_MyViewModel();

		var nameBlock = new TextBlock();
		nameBlock.DataContext = vm;
		nameBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyState.Name") });

		var valueBlock = new TextBlock();
		valueBlock.DataContext = vm;
		valueBlock.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("MyState.Value") });

		var root = new StackPanel { Children = { nameBlock, valueBlock } };
		await UIHelper.Load(root, CT);

		await TestHelper.WaitFor(() => nameBlock.Text == "OriginalName", CT);
		await TestHelper.WaitFor(() => valueBlock.Text == "OriginalValue", CT);

		HotReloadService.UpdateApplication(new[] { typeof(When_UpdateRecordState_Then_BoundTextBlocksUpdated_MyModel_v1) });

		await TestHelper.WaitFor(() => nameBlock.Text == "UpdatedName", CT);
		await TestHelper.WaitFor(() => valueBlock.Text == "UpdatedValue", CT);
		Assert.AreEqual("UpdatedName", nameBlock.Text);
		Assert.AreEqual("UpdatedValue", valueBlock.Text);
	}

	public record When_UpdateRecordState_Then_BoundTextBlocksUpdated_Record(string Name, string Value);

	[ReactiveBindable(true)]
	public partial class When_UpdateRecordState_Then_BoundTextBlocksUpdated_MyModel
	{
		public IState<When_UpdateRecordState_Then_BoundTextBlocksUpdated_Record> MyState
			=> State<When_UpdateRecordState_Then_BoundTextBlocksUpdated_Record>.Value(this, () => new When_UpdateRecordState_Then_BoundTextBlocksUpdated_Record("OriginalName", "OriginalValue"));
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_UpdateRecordState_Then_BoundTextBlocksUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_UpdateRecordState_Then_BoundTextBlocksUpdated_MyModel))]
	public partial class When_UpdateRecordState_Then_BoundTextBlocksUpdated_MyModel_v1 : IAsyncDisposable
	{
		internal When_UpdateRecordState_Then_BoundTextBlocksUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<When_UpdateRecordState_Then_BoundTextBlocksUpdated_Record> MyState
			=> Feed.Async(async ct => new When_UpdateRecordState_Then_BoundTextBlocksUpdated_Record("UpdatedName", "UpdatedValue"));

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion
}
