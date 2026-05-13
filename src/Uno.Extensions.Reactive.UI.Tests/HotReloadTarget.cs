#if DEBUG // Hot-reload target is only relevant in debug configuration
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Bindings;

namespace Uno.Extensions.Reactive.WinUI.Tests;

/// <summary>
/// Helper class for hot reload testing. This class is designed to be updated during hot reload tests.
/// </summary>
[ReactiveBindable(true)]
public partial class HotReloadTargetModel
{
	public IFeed<string> Message => Feed.Async(async ct => "Initial message");
}

/// <summary>
/// Version 1 of HotReloadTargetModel used for hot reload updates.
/// </summary>
[ReactiveBindable(false)]
[Model(bindable: typeof(HotReloadTargetViewModel))]
[MetadataUpdateOriginalType(typeof(HotReloadTargetModel))]
public partial class HotReloadTargetModel_v1 : IAsyncDisposable
{
	internal HotReloadTargetViewModel __reactiveBindableViewModel = default!;

	public IFeed<string> Message => Feed.Async(async ct => "Updated message");

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>
/// Model with multiple feeds for testing hot reload with multiple bindings.
/// </summary>
[ReactiveBindable(true)]
public partial class HotReloadTargetMultipleFeedsModel
{
	public IFeed<string> FirstFeed => Feed.Async(async ct => "First original");
	public IFeed<string> SecondFeed => Feed.Async(async ct => "Second original");
}

/// <summary>
/// Updated version with multiple feeds.
/// </summary>
[ReactiveBindable(false)]
[Model(bindable: typeof(HotReloadTargetMultipleFeedsViewModel))]
[MetadataUpdateOriginalType(typeof(HotReloadTargetMultipleFeedsModel))]
public partial class HotReloadTargetMultipleFeedsModel_v1 : IAsyncDisposable
{
	internal HotReloadTargetMultipleFeedsViewModel __reactiveBindableViewModel = default!;

	public IFeed<string> FirstFeed => Feed.Async(async ct => "First updated");
	public IFeed<string> SecondFeed => Feed.Async(async ct => "Second updated");

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
#endif
