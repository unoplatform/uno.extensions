using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Config;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Dispatching;
using Uno.Extensions.Reactive.Events;
using Uno.Extensions.Reactive.Logging;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// Base class for binding friendly view models.
/// </summary>
/// <remarks>This is not expected to be used by application directly, but by generated code.</remarks>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public abstract partial class BindableViewModelBase : IBindable, INotifyPropertyChanged, IAsyncDisposable
{
	internal static MessageAxis<object?> BindingSource { get; } = new(MessageAxes.BindingSource, _ => null) { IsTransient = true };

	private readonly CompositeAsyncDisposable _disposables = new();
	private readonly AsyncLazyDispatcherProvider _dispatcher = new();
	private readonly EventManager<PropertyChangedEventHandler, PropertyChangedEventArgs> _propertyChanged;

	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged
	{
		add => _propertyChanged.Add(value!);
		remove => _propertyChanged.Remove(value!);
	}

	/// <summary>
	/// Creates a new instance of BindableViewModelBase
	/// </summary>
	protected BindableViewModelBase()
	{
		_propertyChanged = new(this, h => h.Invoke, isCoalescable: false, schedulersProvider: _dispatcher.FindDispatcher);

		_dispatcher.TryResolve();

		InitializeHotReload();
	}

	/// <summary>
	/// Adds a disposable that is going to be disposed with this instance.
	/// </summary>
	/// <param name="disposable">The disposable.</param>
	protected void RegisterDisposable(IAsyncDisposable disposable)
		=> _disposables.Add(disposable);

	/// <summary>
	/// Removes a disposable that is going to be disposed with this instance.
	/// </summary>
	/// <param name="disposable">The disposable.</param>
	protected void UnregisterDisposable(IAsyncDisposable disposable)
		=> _disposables.Remove(disposable);

	/// <summary>
	/// Raise the property changed event for the given property name.
	/// </summary>
	/// <param name="propertyName">Name of teh property, or nothing to let compiler full-fil it.</param>
	protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
		=> _propertyChanged.Raise(new PropertyChangedEventArgs(propertyName));

	/// <summary>
	/// Get info for a bindable property given a backing feed.
	/// </summary>
	/// <typeparam name="TProperty">The type of the sub-property.</typeparam>
	/// <param name="propertyName">The name of the sub-property.</param>
	/// <param name="feed">The backing state of the property.</param>
	/// <returns>Info that can be used to create a bindable object.</returns>
	protected BindablePropertyInfo<TProperty> Property<TProperty>(string propertyName, IFeed<TProperty> feed)
	{
		var ctx = SourceContext.Find(this) ?? throw new InvalidOperationException(
			"This must be invoked only after the SourceContext of the parent as been forwarded to this."
			+ "This method should be used only by generated code, consider to remove usage in your app.");

		var state = ctx.GetOrCreateState(feed);

		return CreateProperty(propertyName, (StateImpl<TProperty>)state, isReadOnly: feed != state);
	}

	/// <summary>
	/// Get info for a bindable property given a backing state.
	/// </summary>
	/// <typeparam name="TProperty">The type of the sub-property.</typeparam>
	/// <param name="propertyName">The name of the sub-property.</param>
	/// <param name="state">The backing state of the property.</param>
	/// <returns>Info that can be used to create a bindable object.</returns>
	protected BindablePropertyInfo<TProperty> Property<TProperty>(string propertyName, IState<TProperty> state)
	{
		if (state is not StateImpl<TProperty> impl)
		{
			throw new InvalidOperationException("Custom implementation of state are not supported yet.");
		}

		return CreateProperty(propertyName, impl, isReadOnly: false);
	}


	/// <summary>
	/// LEGACY support for IInput&lt;T&gt; - Get info for a bindable property.
	/// </summary>
	/// <typeparam name="TProperty">The type of the sub-property.</typeparam>
	/// <param name="propertyName">The name of the sub-property.</param>
	/// <param name="initialValue">The default value of the property.</param>
	/// <param name="state">The backing state of the property.</param>
	/// <returns>Info that can be used to create a bindable object.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)] // Legacy
	protected BindablePropertyInfo<TProperty> Property<TProperty>(string propertyName, TProperty? initialValue, out IInput<TProperty> state)
	{
		initialValue ??= GetDefaultValueForBindings<TProperty>();

		var stateImpl = new StateImpl<TProperty>(Option.Some(initialValue));
		var info = CreateProperty(propertyName, stateImpl, isReadOnly: false);

		_disposables.Add(stateImpl);
		state = new Input<TProperty>(propertyName, stateImpl);

		return info;
	}

	private BindablePropertyInfo<TProperty> CreateProperty<TProperty>(string propertyName, StateImpl<TProperty> stateImpl, bool isReadOnly)
	{
		return new BindablePropertyInfo<TProperty>(this, propertyName, (stateImpl, ViewModelToView), isReadOnly ? default : ViewToViewModel);

		async void ViewModelToView(Action<TProperty> updated)
		{
			try
			{
				var defaultValue = GetDefaultValueForBindings<TProperty>();
				var value = stateImpl.Current.Current.Data.SomeOrDefault(defaultValue);

				// We run the update sync in setup, no matter the thread
				updated(value);

				var ct = stateImpl.Context.Token;
				var source = FeedUIHelper.GetSource(stateImpl, stateImpl.Context);
				var dispatcher = await _dispatcher.GetFirstResolved(ct).ConfigureAwait(false);
				var updateScheduled = 0;

				// Note: We use for each here to deduplicate updates in case of fast updates of the source.
				//		 This also ensure to not wait to for the UI thread before fetching MoveNext the source.
				_ = source
					.ForEachAsync(OnMessage, ct)
					.ContinueWith(
						enumeration =>
						{
							this.Log().Error(
								enumeration.Exception!,
								$"Synchronization from ViewModel to View of '{propertyName}' failed."
								+ "(This is a final error, changes made in the VM are no longer propagated to the View.)");
						},
						TaskContinuationOptions.OnlyOnFaulted);
				void OnMessage(Message<TProperty> msg)
				{
					if (msg.Changes.Contains(MessageAxis.Data) && !(msg.Current.Get(BindingSource)?.Equals((this, propertyName)) ?? false))
					{
						value = msg.Current.Data.SomeOrDefault(defaultValue);
						if (Interlocked.CompareExchange(ref updateScheduled, 1, 0) is 0)
						{
							dispatcher.TryEnqueue(UpdateValue);
						}
					}
				}

				void UpdateValue()
				{
					updateScheduled = 0;
					updated(value);
					_propertyChanged.Raise(new PropertyChangedEventArgs(propertyName));
				}
			}
			catch (Exception error)
			{
				this.Log().Error(
					error,
					$"Synchronization from ViewModel to View of '{propertyName}' failed."
					+ "(This is a final error, changes made in the VM are no longer propagated to the View.)");
			}
		}

		async ValueTask ViewToViewModel(Func<TProperty, TProperty> updater, bool isLeafPropertyChanged, CancellationToken ct)
		{
			// 1. Notify the View that the property has been updated
			if (isLeafPropertyChanged)
			{
				_propertyChanged.Raise(new PropertyChangedEventArgs(propertyName));
			}

			// 2. Asynchronously update the backing state, specifying the BindingSource, so we avoid re-entrancy with the ViewModelToView
			// Here we also make sure to leave the UI thread so no matter the implementation of the State,
			// we won't raise the State updated callbacks on the UI Thread.
			await Task
				.Run(async () => await stateImpl.UpdateMessageAsync(DoUpdate, ct).ConfigureAwait(false), ct)
				.ConfigureAwait(false);

			void DoUpdate(MessageBuilder<TProperty> msg)
			{
				var current = msg.CurrentData.SomeOrDefault(GetDefaultValueForBindings<TProperty>());
				var updated = updater(current);

				msg.Data(Option.Some(updated)).Set(BindingSource, (this, propertyName));
			}
		}
	}

	private static T GetDefaultValueForBindings<T>()
		=> default!; // For now we default to null as we cannot enforce non-null through bindings

	/// <summary>
	/// Gets a logger for reactive generated code.
	/// </summary>
	/// <returns>An ILogger than can be used by generated code to log some messages.</returns>
	[EditorBrowsable(EditorBrowsableState.Never)]
	protected ILogger __Reactive_Log()
		=> this.Log();

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await _disposables.DisposeAsync().ConfigureAwait(false);
		_propertyChanged.Dispose();
		_dispatcher.Dispose();
	}
}
