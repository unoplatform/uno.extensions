using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Dispatching;

namespace Uno.Extensions.Reactive.Testing;

/// <summary>
/// Base class to test a class that is using the reactive framework from the UI.
/// </summary>
public class FeedUITests : FeedTests, ISourceContextOwner
{
	private (TestDispatcher Value, FindDispatcher Resolve)? _testDispatcher;
	private IDispatcher? _realDispatcher;

	string ISourceContextOwner.Name => ToString()!;
	IDispatcher ISourceContextOwner.Dispatcher => Dispatcher;

	/// <summary>
	/// The dispatcher associated to the thread which abstracts the UI thread in tests.
	/// </summary>
	public IDispatcher Dispatcher => (_realDispatcher ??= DispatcherHelper.GetForCurrentThread())
		?? _testDispatcher?.Value
		?? throw new InvalidOperationException("The dispatcher has not been initialized yet. Consider adding the [RunsOnUIThread] attribute on your test class.");

	/// <inheritdoc />
	[TestInitialize]
	public override void Initialize()
	{
		base.Initialize();

		if (DispatcherHelper.GetForCurrentThread == DispatcherHelper.NotConfigured)
		{
			var dispatcher = new TestDispatcher(TestContext?.TestName);
			_testDispatcher = new(dispatcher, () => dispatcher.HasThreadAccess ? dispatcher : null);
			DispatcherHelper.GetForCurrentThread = _testDispatcher.Value.Resolve;
		}
		else
		{
			_realDispatcher = DispatcherHelper.GetForCurrentThread();
		}
	}

	/// <inheritdoc />
	[TestCleanup]
	public override void Cleanup()
	{
		if (_testDispatcher is {} testDispatcher)
		{ 
			_testDispatcher = null;
			if (DispatcherHelper.GetForCurrentThread == testDispatcher.Resolve)
			{
				DispatcherHelper.GetForCurrentThread = DispatcherHelper.NotConfigured;
			}
			testDispatcher.Value.Dispose();
		}

		base.Cleanup();
	}

	/// <summary>
	/// Creates a <see cref="SourceContext"/> which contains UI thread information.
	/// </summary>
	/// <returns>A <see cref="SourceContext"/> which contains UI thread information</returns>
	protected SourceContext CreateUIContext()
		=> Context.SourceContext.CreateChild(this, new RequestSource());

	private protected async Task<T> WaitForInitialValue<TViewModel, T>(TViewModel viewModel, Func<TViewModel, Bindable<T>> propertySelector, CancellationToken ct = default)
		where TViewModel : BindableViewModelBase
		where T : class
	{
		if (!ct.CanBeCanceled)
		{
			ct = CT;
		}

		var tcs = new TaskCompletionSource<T>();
		using var _ = ct.Register(() => tcs.TrySetCanceled());
		await ExecuteOnDispatcher(() =>
			{
				var bindable = propertySelector(viewModel);

				// Note: This is a patch since the implementation of IFeed by the bindable is only a VM.SrcFeed.Select(getter).
				//		 It should be a real implementation that synchronously reflects the current value of the bindable itself.
				var currentValue = bindable.GetValue();
				if (currentValue != null)
				{
					// This is a weak test since we could have a feed that init with null. But it's enough for now, regarding the comment above !
					tcs.TrySetResult(currentValue);
					return;
				}

				// Adding the event handler will also init the dispatcher
				var ctReg = default(CancellationTokenRegistration);
				viewModel.PropertyChanged += PropertyChanged;
				ctReg = ct.Register(() => viewModel.PropertyChanged -= PropertyChanged);

				void PropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
				{
					if (propertyChangedEventArgs.PropertyName == bindable.PropertyName)
					{
						ctReg.Dispose();
						viewModel.PropertyChanged -= PropertyChanged;
						tcs.TrySetResult(bindable.GetValue());
					}
				}
			},
			ct);

		return await tcs.Task;
	}

	/// <summary>
	/// Asynchronously executes an action on the UI thread.
	/// </summary>
	/// <param name="action">The action to execute</param>
	/// <param name="ct">A cancellation to cancel the async operation, if ignored, the <see cref="FeedTests.CT"/> will be used.</param>
	/// <returns>An action operation which indicates the end of the execution.</returns>
	protected async ValueTask ExecuteOnDispatcher(Action action, CancellationToken ct = default)
	{
		if (!ct.CanBeCanceled)
		{
			ct = CT;
		}

		var tcs = new TaskCompletionSource<object?>();
		using var _ = ct.Register(() => tcs.TrySetCanceled());
		Dispatcher.TryEnqueue(() =>
		{
			action();
			tcs.TrySetResult(default);
		});

		await tcs.Task;
	}

	/// <summary>
	/// Asynchronously executes an action on the UI thread.
	/// </summary>
	/// <param name="action">The action to execute</param>
	/// <param name="ct">A cancellation to cancel the async operation, if ignored, the <see cref="FeedTests.CT"/> will be used.</param>
	/// <returns>An action operation which indicates the end of the execution.</returns>
	protected async ValueTask<T> ExecuteOnDispatcher<T>(Func<T> action, CancellationToken ct = default)
	{
		if (!ct.CanBeCanceled)
		{
			ct = CT;
		}

		var tcs = new TaskCompletionSource<T>();
		using var _ = ct.Register(() => tcs.TrySetCanceled());
		Dispatcher.TryEnqueue(() =>
		{
			var t = action();
			tcs.TrySetResult(t);
		});

		return await tcs.Task;
	}

	/// <summary>
	/// Asynchronously executes an async action on the UI thread.
	/// </summary>
	/// <param name="action">The action to execute</param>
	/// <param name="ct">A cancellation to cancel the async operation, if ignored, the <see cref="FeedTests.CT"/> will be used.</param>
	/// <returns>An action operation which indicates the end of the execution.</returns>
	protected async ValueTask ExecuteAsyncOnDispatcher(AsyncAction action, CancellationToken ct = default)
	{
		if (!ct.CanBeCanceled)
		{
			ct = CT;
		}

		var tcs = new TaskCompletionSource<object?>();
		using var _ = ct.Register(() => tcs.TrySetCanceled());
		Dispatcher.TryEnqueue(async () =>
		{
			await action(ct);
			tcs.TrySetResult(default);
		});

		await tcs.Task;
	}

	/// <summary>
	/// Asynchronously executes an async action on the UI thread.
	/// </summary>
	/// <param name="action">The action to execute</param>
	/// <param name="ct">A cancellation to cancel the async operation, if ignored, the <see cref="FeedTests.CT"/> will be used.</param>
	/// <returns>An action operation which indicates the end of the execution.</returns>
	protected async ValueTask<T> ExecuteAsyncOnDispatcher<T>(AsyncFunc<T> action, CancellationToken ct = default)
	{
		if (!ct.CanBeCanceled)
		{
			ct = CT;
		}

		var tcs = new TaskCompletionSource<T>();
		using var _ = ct.Register(() => tcs.TrySetCanceled());
		Dispatcher.TryEnqueue(async () =>
		{
			var t = await action(ct);
			tcs.TrySetResult(t);
		});

		return await tcs.Task;
	}
}
