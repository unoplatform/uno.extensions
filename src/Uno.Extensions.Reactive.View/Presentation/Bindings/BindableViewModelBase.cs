using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Uno.Extensions.Reactive.Utils;
using Uno.Extensions.Reactive.View.Utils;
using Uno.Logging;

namespace Uno.Extensions.Reactive;

[EditorBrowsable(EditorBrowsableState.Advanced)]
public abstract partial class BindableViewModelBase : IBindable, INotifyPropertyChanged, IAsyncDisposable
{
	internal static MessageAxis<object?> BindingSource { get; } = new(MessageAxises.BindingSource, _ => null);

	public event PropertyChangedEventHandler? PropertyChanged;

	private readonly CompositeAsyncDisposable _disposables = new();

	protected void RegisterDisposable(IAsyncDisposable disposable) 
		=> _disposables.Add(disposable);

	protected BindablePropertyInfo<TProperty> Property<TProperty>(string propertyName, TProperty? defaultValue, out IState<TProperty> state, DispatcherQueue? dispatcher = null)
	{
		var stateImpl = new State<TProperty>(defaultValue);
		var info = new BindablePropertyInfo<TProperty>(this, propertyName, ViewModelToView, ViewToViewModel);

		_disposables.Add(stateImpl);
		state = new Input<TProperty>(propertyName, stateImpl);

		return info;

		void ViewModelToView(Action<TProperty?> updated)
			=> DispatcherHelper.GetDispatcher(dispatcher).TryEnqueue(async () =>
			{
				try
				{
					updated(defaultValue);

					// Note: No needs to use .WithCancellation() here as we are enumerating the stateImp which is going to be disposed anyway.
					await foreach (var msg in stateImpl.GetSource().ConfigureAwait(true))
					{
						if (msg.Current.Get(BindingSource) != this)
						{
							updated(msg.Current.Data.SomeOrDefault());
						}
					}
				}
				catch (Exception error)
				{
					this.Log().Error(
						$"Synchronization from ViewModel to View of '{propertyName}' failed."
						+ "(This is a final error, changes made in the VM are no longer propagated to the View.)", 
						error);
				}
			});

		async ValueTask ViewToViewModel(Func<TProperty?, TProperty?> updater, bool isLeafPropertyChanged, CancellationToken ct)
		{
			// 1. Notify the View that the property has been updated
			if (isLeafPropertyChanged)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
			}

			// 2. Asynchronously update the backing state, specifying the BindingSource, so we avoid re-entrancy with the ViewModelToView
			// Here we also make sure to leave the UI thread so no matter the implementation of the State,
			// we won't raise the State updated callbacks on the UI Thread.
			await Task
				.Run(async () => await stateImpl.Update(DoUpdate, ct).ConfigureAwait(false), ct)
				.ConfigureAwait(false);

			MessageBuilder<TProperty> DoUpdate(Message<TProperty> msg)
			{
				var current = msg.Current.Data.SomeOrDefault();
				var updated = updater(current);

				return msg.With().Data(Option.Some(updated)).Set(BindingSource, this);
			}
		}
	}

	protected ICommandBuilder CreateCommand(string propertyName)
		=> new CommandBuilder<object?>(propertyName);

	protected ICommandBuilder<T> CreateCommand<T>(string propertyName)
		=> new CommandBuilder<T>(propertyName);

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> _disposables.DisposeAsync();
}

internal class Input<T> : IState<T>
{
	private readonly IState<T> _state;

	public string PropertyName { get; }

	public Input(string propertyName, IState<T> state)
	{
		_state = state;
		PropertyName = propertyName;
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
		=> _state.GetSource(context, ct);

	/// <inheritdoc />
	public ValueTask Update(Func<Message<T>, MessageBuilder<T>> updater, CancellationToken ct)
		=> _state.Update(msg => updater(msg).Set(BindableViewModelBase.BindingSource, this), ct);

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> _state.DisposeAsync();
}
