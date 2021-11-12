using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI.Core;
using Uno.Extensions;
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

	protected BindablePropertyInfo<TProperty> Property<TProperty>(string propertyName, out IState<TProperty> state, DispatcherQueue? dispatcher = null)
		=> Property(propertyName, default, out state, dispatcher);

	protected BindablePropertyInfo<TProperty> Property<TProperty>(string propertyName, TProperty? defaultValue, out IState<TProperty> state, DispatcherQueue? dispatcher = null)
	{
		var stateImpl = new State<TProperty>(defaultValue is null ? Option.Undefined<TProperty>() : defaultValue);
		var info = new BindablePropertyInfo<TProperty>(this, propertyName, ViewModelToView, ViewToViewModel);

		_disposables.Add(stateImpl);
		state = new Input<TProperty>(propertyName, stateImpl);

		return info;

		void ViewModelToView(Action<TProperty?> updated)
			=> DispatcherHelper.GetDispatcher(dispatcher).TryEnqueue(async () =>
			{
				// Note: No needs to use .WithCancellation() here as we are enumerating the stateImp which is going to be disposed anyway.
				try
				{
					updated(defaultValue);
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

		ValueTask ViewToViewModel(Func<TProperty?, TProperty?> updater, CancellationToken ct)
			=> stateImpl.Update(
				msg =>
				{
					var current = msg.Current.Data.SomeOrDefault();
					var updated = updater(current);

					return msg.With().Data(Option.Some(updated)).Set(BindingSource, this);
				},
				ct);
	}

	protected ICommandBuilder CreateCommand(string propertyName)
		=> new CommandBuilder<object?>(propertyName);

	void IBindable.OnPropertyChanged(string propertyName) 
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
