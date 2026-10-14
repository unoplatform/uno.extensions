using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Config;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Core.HotReload;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.IntegrationTests;

[TestClass]
public partial class Given_HotReload : FeedUITests
{
	/// <inheritdoc />
	[TestInitialize]
	public override void Initialize()
	{
		FeedConfiguration.HotReload = HotReloadSupport.Enabled;
		typeof(FeedConfiguration).GetField("_effectiveHotReload", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, null);

		Console.WriteLine("Hot reload configuration is now : " + FeedConfiguration.EffectiveHotReload);

		base.Initialize();
	}

	/// <inheritdoc />
	[TestCleanup]
	public override void Cleanup()
	{
		FeedConfiguration.HotReload = null;
		typeof(FeedConfiguration).GetField("_effectiveHotReload", BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, null);
		FeedConfiguration.HotReloadRemovalBehavior = HotReloadRemovalBehavior.KeepPrevious;

		Console.WriteLine("Hot reload configuration has been restored to : " + FeedConfiguration.EffectiveHotReload);

		base.Cleanup();
	}

	[TestMethod]
	public async Task When_SharedSubscriptionHotSwapped_Then_StateAndOperatorFollow()
	{
		using var context = new FeedTestContext();
		context.RestoreCurrent();
		var original = Feed.Async(async ct => "original");
		var state = context.SourceContext.GetOrCreateState(original);
		var derived = original.Select(value => $"{value}-derived");
		var (stateValues, _) = state.Record();
		var (derivedValues, _) = derived.Record();

		await stateValues.WaitForData("original");
		await derivedValues.WaitForData("original-derived");

		var replacement = Feed.Async(async ct => "replacement");
		var subscription = context.SourceContext.States.GetOrCreateSubscription(original);
		subscription.CanHotSwap.Should().BeTrue();
		subscription.HotSwap(replacement);
		context.SourceContext.States.SetSubscription(replacement, subscription);

		context.SourceContext.States.GetOrCreateSubscription(replacement).Should().BeSameAs(subscription);
		await stateValues.WaitForData("replacement");
		await derivedValues.WaitForData("replacement-derived");
	}

	#region When_UpdateValueTypeFeedInModel_Then_BindableUpdated
	[TestMethod]
	public async Task When_UpdateValueTypeFeedInModel_Then_BindableUpdated()
	{
		var bindable = new When_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyViewModel();

		var tcs = new TaskCompletionSource();
		Dispatcher.TryEnqueue(() => bindable.PropertyChanged += (s, e) => tcs.TrySetResult());

		await WaitFor(() => bindable.MyFeed, "Feed value from original model");

		HotReloadService.UpdateApplication(new[] { typeof(When_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyModel_v1) });

		await tcs.Task;
		await WaitFor(() => bindable.MyFeed, "Feed value from model v1");
	}

	[ReactiveBindable(true)]
	public partial class When_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyModel
	{
		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from original model");
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyModel))]
	public partial class When_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyModel_v1 : IAsyncDisposable
	{
		internal When_UpdateValueTypeFeedInModel_Then_BindableUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from model v1");

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion

	#region When_UpdateRecordFeedInModel_Then_BindableUpdated
	[TestMethod]
	public async Task When_UpdateRecordFeedInModel_Then_BindableUpdated()
	{
		var bindable = new When_UpdateRecordFeedInModel_Then_BindableUpdated_MyViewModel();

		var tcs = new TaskCompletionSource();
		Dispatcher.TryEnqueue(() => bindable.PropertyChanged += (s, e) => tcs.TrySetResult());

		await WaitFor(() => bindable.MyFeed.Value, "Feed value from original model");

		HotReloadService.UpdateApplication(new[] { typeof(When_UpdateRecordFeedInModel_Then_BindableUpdated_MyModel_v1) });

		await tcs.Task;
		await WaitFor(() => bindable.MyFeed.Value, "Feed value from model v1");
	}

	public record When_UpdateRecordFeedInModel_Then_BindableUpdated_Record(string Value);

	[ReactiveBindable(true)]
	public partial class When_UpdateRecordFeedInModel_Then_BindableUpdated_MyModel
	{
		public IFeed<When_UpdateRecordFeedInModel_Then_BindableUpdated_Record> MyFeed => Feed.Async(async ct => new When_UpdateRecordFeedInModel_Then_BindableUpdated_Record("Feed value from original model"));
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_UpdateRecordFeedInModel_Then_BindableUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_UpdateRecordFeedInModel_Then_BindableUpdated_MyModel))]
	public partial class When_UpdateRecordFeedInModel_Then_BindableUpdated_MyModel_v1 : IAsyncDisposable
	{
		internal When_UpdateRecordFeedInModel_Then_BindableUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<When_UpdateRecordFeedInModel_Then_BindableUpdated_Record> MyFeed => Feed.Async(async ct => new When_UpdateRecordFeedInModel_Then_BindableUpdated_Record("Feed value from model v1"));

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion

	#region When_ChangeKindOfValueTypeFeedInModel_Then_BindableUpdated
	[TestMethod]
	public async Task When_ChangeKindOfValueTypeFeedInModel_Then_BindableUpdated()
	{
		var bindable = new When_ChangeKindOfValueTypeFeedInModel_Then_BindableUpdated_MyViewModel();

		var tcs = new TaskCompletionSource();
		Dispatcher.TryEnqueue(() => bindable.PropertyChanged += (s, e) => tcs.TrySetResult());

		await WaitFor(() => bindable.MyFeed, "Feed value from original model");

		HotReloadService.UpdateApplication(new[] { typeof(When_ChangeKindOfValueTypeFeedInModel_Then_BindableUpdated_MyModel_v1) });

		await tcs.Task;
		await WaitFor(() => bindable.MyFeed, "Feed value from model v1");
	}

	[ReactiveBindable(true)]
	public partial class When_ChangeKindOfValueTypeFeedInModel_Then_BindableUpdated_MyModel
	{
		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from original model");
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_ChangeKindOfValueTypeFeedInModel_Then_BindableUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_ChangeKindOfValueTypeFeedInModel_Then_BindableUpdated_MyModel))]
	public partial class When_ChangeKindOfValueTypeFeedInModel_Then_BindableUpdated_MyModel_v1 : IAsyncDisposable
	{
		internal When_ChangeKindOfValueTypeFeedInModel_Then_BindableUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyFeed => Feed.Dynamic(async ct => "Feed value from model v1");

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion

	#region When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated
	[TestMethod]
	public async Task When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated()
	{
		var bindable = new When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated_MyViewModel();

		var tcs = new TaskCompletionSource();
		Dispatcher.TryEnqueue(() => bindable.PropertyChanged += (s, e) => tcs.TrySetResult());

		await WaitFor(() => bindable.MyFeed.Value, "Feed value from original model");

		HotReloadService.UpdateApplication(new[] { typeof(When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated_MyModel_v1) });

		await tcs.Task;
		await WaitFor(() => bindable.MyFeed.Value, "Feed value from model v1");
	}

	public record When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated_Record(string Value);

	[ReactiveBindable(true)]
	public partial class When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated_MyModel
	{
		public IFeed<When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated_Record> MyFeed => Feed.Async(async ct => new When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated_Record("Feed value from original model"));
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated_MyModel))]
	public partial class When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated_MyModel_v1 : IAsyncDisposable
	{
		internal When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated_Record> MyFeed => Feed.Dynamic(async ct => new When_ChangeKindOfRecordFeedInModel_Then_BindableUpdated_Record("Feed value from model v1"));

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion

	#region When_AddAndUpdateFeedInModel_Then_BindableUpdated
	[TestMethod]
	[Ignore("This test requires actual HR capabilities (for the VM to be updated also), but the test When_RemoveAndRestoreFeedInModel_Then_BindableUpdated does validate same code path")]
	public async Task When_AddAndUpdateFeedInModel_Then_BindableUpdated()
	{
		var bindable = new When_AddAndUpdateFeedInModel_Then_BindableUpdated_MyViewModel();

		var tcs = new TaskCompletionSource();
		Dispatcher.TryEnqueue(() => bindable.PropertyChanged += (s, e) => tcs.TrySetResult());

		await WaitFor(() => bindable.MyFeed, "Feed value from original model");

		HotReloadService.UpdateApplication(new[] { typeof(When_AddAndUpdateFeedInModel_Then_BindableUpdated_v1) });

		await tcs.Task;
		await WaitFor(() => bindable.MyFeed, "Feed value from model v1");
		await WaitFor(() => bindable.GetType().GetProperty("MySecondFeed")?.GetValue(bindable), "Second feed value from model v1");

		tcs = new TaskCompletionSource();
		HotReloadService.UpdateApplication(new[] { typeof(When_AddAndUpdateFeedInModel_Then_BindableUpdated_v2) });

		await tcs.Task;
		await WaitFor(() => bindable.MyFeed, "Feed value from model v2");
		await WaitFor(() => bindable.GetType().GetProperty("MySecondFeed")?.GetValue(bindable), "Second feed value from model v2");
	}

	[ReactiveBindable(true)]
	public partial class When_AddAndUpdateFeedInModel_Then_BindableUpdated_MyModel
	{
		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from original model");
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_AddAndUpdateFeedInModel_Then_BindableUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_AddAndUpdateFeedInModel_Then_BindableUpdated_MyModel))]
	public partial class When_AddAndUpdateFeedInModel_Then_BindableUpdated_v1 : IAsyncDisposable
	{
		internal When_AddAndUpdateFeedInModel_Then_BindableUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from model v1");
		public IFeed<string> MySecondFeed => Feed.Async(async ct => "Second feed value from model v1");
		
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_AddAndUpdateFeedInModel_Then_BindableUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_AddAndUpdateFeedInModel_Then_BindableUpdated_MyModel))]
	public partial class When_AddAndUpdateFeedInModel_Then_BindableUpdated_v2 : IAsyncDisposable
	{
		internal When_AddAndUpdateFeedInModel_Then_BindableUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from model v2");
		public IFeed<string> MySecondFeed => Feed.Async(async ct => "Second feed value from model v2");
		
		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion

	#region When_RemoveAndRestoreFeedInModel_Then_BindableUpdated
	[TestMethod]
	public async Task When_RemoveAndRestoreFeedInModel_Then_BindableUpdated()
	{
		FeedConfiguration.HotReloadRemovalBehavior = HotReloadRemovalBehavior.Clear;
		var bindable = new When_RemoveAndRestoreFeedInModel_Then_BindableUpdated_MyViewModel();

		var tcs = new TaskCompletionSource();
		Dispatcher.TryEnqueue(() => bindable.PropertyChanged += (s, e) => tcs.TrySetResult());

		await WaitFor(() => bindable.MyFeed, "Feed value from original model");
		await WaitFor(() => bindable.MySecondFeed, "Second value from original model");

		HotReloadService.UpdateApplication(new[] { typeof(When_RemoveAndRestoreFeedInModel_Then_BindableUpdated_v1) });

		await tcs.Task;
		await WaitFor(() => bindable.MyFeed, "Feed value from model v1");
		await WaitFor(() => bindable.MySecondFeed, null);

		tcs = new TaskCompletionSource();
		HotReloadService.UpdateApplication(new[] { typeof(When_RemoveAndRestoreFeedInModel_Then_BindableUpdated_v2) });

		await tcs.Task;
		await WaitFor(() => bindable.MyFeed, "Feed value from model v2");
		await WaitFor(() => bindable.MySecondFeed, "Second feed value from model v2");
	}

	[ReactiveBindable(true)]
	public partial class When_RemoveAndRestoreFeedInModel_Then_BindableUpdated_MyModel
	{
		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from original model");
		public IFeed<string> MySecondFeed => Feed.Async(async ct => "Second value from original model");
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_RemoveAndRestoreFeedInModel_Then_BindableUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_RemoveAndRestoreFeedInModel_Then_BindableUpdated_MyModel))]
	public partial class When_RemoveAndRestoreFeedInModel_Then_BindableUpdated_v1 : IAsyncDisposable
	{
		internal When_RemoveAndRestoreFeedInModel_Then_BindableUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from model v1");

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_RemoveAndRestoreFeedInModel_Then_BindableUpdated_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_RemoveAndRestoreFeedInModel_Then_BindableUpdated_MyModel))]
	public partial class When_RemoveAndRestoreFeedInModel_Then_BindableUpdated_v2 : IAsyncDisposable
	{
		internal When_RemoveAndRestoreFeedInModel_Then_BindableUpdated_MyViewModel __reactiveBindableViewModel = default!;

		public IFeed<string> MyFeed => Feed.Async(async ct => "Feed value from model v2");
		public IFeed<string> MySecondFeed => Feed.Async(async ct => "Second feed value from model v2");

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
	#endregion

	#region When_AddDerivedFeedInModel_Then_StateBindableFollowsUpdatedModel
	// Repro for https://github.com/unoplatform/uno.extensions/issues/3226:
	// an HR delta that adds a feed derived from an existing state (`Count.Select(...)`) must not leave
	// the `Count` bindable on the previous model's state while commands update the new model's state.
	[TestMethod]
	public async Task When_AddDerivedFeedInModel_Then_StateBindableFollowsUpdatedModel()
	{
		var bindable = new When_AddDerivedFeedInModel_MyViewModel_Updated();

		var tcs = new TaskCompletionSource();
		Dispatcher.TryEnqueue(() => bindable.PropertyChanged += (s, e) => tcs.TrySetResult());

		bindable.Increment.Execute(null);
		await WaitFor(() => bindable.Count == 1);

		HotReloadService.UpdateApplication(new[] { typeof(When_AddDerivedFeedInModel_MyModel_v1) });

		await tcs.Task;
		When_AddDerivedFeedInModel_MyModel_v1.LastInstance.Should().NotBeNull();

		// The bindable now follows the updated model's state (which starts at 0) ...
		await WaitFor(() => bindable.Count == 0);

		// ... and the command, rebuilt against the updated model, updates that same state.
		bindable.Increment.Execute(null);
		bindable.Increment.Execute(null);
		await WaitFor(() => bindable.Count == 2);

		// The feed added by the delta is live and derives from the state the command updates.
		for (var i = 3; i <= 10; i++)
		{
			bindable.Increment.Execute(null);
			await WaitFor(() => bindable.Count == i);
		}
		await WaitFor(() => bindable.IsGoalReached);

		// A second delta must still find the shared subscription (the reason the patcher runs before the initializer).
		tcs = new TaskCompletionSource();
		HotReloadService.UpdateApplication(new[] { typeof(When_AddDerivedFeedInModel_MyModel_v2) });

		await tcs.Task;
		await WaitFor(() => bindable.Count == 0);
		await WaitFor(() => !bindable.IsGoalReached);

		bindable.Increment.Execute(null);
		await WaitFor(() => bindable.Count == 1);
	}

	[ReactiveBindable(true)]
	public partial class When_AddDerivedFeedInModel_MyModel
	{
		public IState<int> Count => State.Value(this, () => 0);

		public async ValueTask Increment(CancellationToken ct) => await Count.UpdateAsync(c => c + 1, ct);
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_AddDerivedFeedInModel_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_AddDerivedFeedInModel_MyModel))]
	public partial class When_AddDerivedFeedInModel_MyModel_v1 : IAsyncDisposable
	{
		public static When_AddDerivedFeedInModel_MyModel_v1? LastInstance { get; private set; }

		internal When_AddDerivedFeedInModel_MyViewModel __reactiveBindableViewModel = default!;

		public When_AddDerivedFeedInModel_MyModel_v1() => LastInstance = this;

		public IState<int> Count => State.Value(this, () => 0);

		public IFeed<bool> IsGoalReached => Count.Select(c => c >= 10);

		public async ValueTask Increment(CancellationToken ct) => await Count.UpdateAsync(c => c + 1, ct);

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	[ReactiveBindable(false)]
	[Model(bindable: typeof(When_AddDerivedFeedInModel_MyViewModel))]
	[MetadataUpdateOriginalType(typeof(When_AddDerivedFeedInModel_MyModel))]
	public partial class When_AddDerivedFeedInModel_MyModel_v2 : IAsyncDisposable
	{
		internal When_AddDerivedFeedInModel_MyViewModel __reactiveBindableViewModel = default!;

		public IState<int> Count => State.Value(this, () => 0);

		public IFeed<bool> IsGoalReached => Count.Select(c => c >= 10);

		public async ValueTask Increment(CancellationToken ct) => await Count.UpdateAsync(c => c + 1, ct);

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}

	// Emulates the EnC-updated generated view model: the real delta rewrites `__Reactive_BindableInitializeForUpdatedModel`
	// in place so it also initializes the newly added `IsGoalReached` member.
	public class When_AddDerivedFeedInModel_MyViewModel_Updated : When_AddDerivedFeedInModel_MyViewModel
	{
		private Bindable<bool>? _isGoalReached;

		public bool IsGoalReached => _isGoalReached?.GetValue() ?? false;

		protected override void __Reactive_BindableInitializeForUpdatedModel(object updatedModel, SourceContext ctx)
		{
			base.__Reactive_BindableInitializeForUpdatedModel(updatedModel, ctx);

			dynamic model = updatedModel;
			_isGoalReached ??= new Bindable<bool>(Property<bool>(nameof(IsGoalReached), (IFeed<bool>)model.IsGoalReached));
		}
	}
	#endregion

	private async Task WaitFor(Func<bool> predicate)
	{
		for (var i = 0; i < 100; i++)
		{
			if (predicate())
			{
				return;
			}

			await Task.Delay(1);
		}

		throw new TimeoutException();
	}

	private async Task WaitFor<T>(Func<T?> actual, T? expected)
		where T : class
	{
		const int attempts = 100;
		for (var i = 0; i < attempts; i++)
		{
			try
			{
				if (actual() == expected)
				{
					return;
				}
			}
			catch { }

			await Task.Delay(3);
		}

		throw new TimeoutException($"Expected '{expected}' but get '{actual()}' after {attempts}ms.");
	}
}
