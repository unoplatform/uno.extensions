using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Uno.Extensions.Reactive.Utils;
using Uno.Logging;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// Base class for binding friendly view models.
/// </summary>
/// <remarks>This is not expected to be used by application directly, but by generated code.</remarks>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public abstract partial class BindableViewModelBase : IBindable, INotifyPropertyChanged, IAsyncDisposable
{
	internal static MessageAxis<object?> BindingSource { get; } = new(MessageAxes.BindingSource, _ => null);

	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	private readonly CompositeAsyncDisposable _disposables = new();

	/// <summary>
	/// Adds a disposable that is going to be disposed with this instance.
	/// </summary>
	/// <param name="disposable">The disposable.</param>
	protected void RegisterDisposable(IAsyncDisposable disposable) 
		=> _disposables.Add(disposable);

	/// <summary>
	/// Get info for a bindable property.
	/// </summary>
	/// <typeparam name="TProperty">The type of the sub-property.</typeparam>
	/// <param name="propertyName">The name of the sub-property.</param>
	/// <param name="defaultValue">The default value of the property.</param>
	/// <param name="state">The backing state of the property.</param>
	/// <param name="dispatcher">The dispatcher to use to raise property changes.</param>
	/// <returns>Info that can be used to create a bindable object.</returns>
	protected BindablePropertyInfo<TProperty> Property<TProperty>(string propertyName, TProperty? defaultValue, out IInput<TProperty> state, DispatcherQueue? dispatcher = null)
	{
		var stateImpl = new State<TProperty>(defaultValue);
		var info = new BindablePropertyInfo<TProperty>(this, propertyName, ViewModelToView, ViewToViewModel);

		_disposables.Add(stateImpl);
		state = new Input<TProperty>(propertyName, stateImpl);

		return info;

		void ViewModelToView(Action<TProperty?> updated)
			=> DispatcherHelper.TryEnqueue(dispatcher, async () =>
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

	/// <inheritdoc />
	public ValueTask DisposeAsync()
		=> _disposables.DisposeAsync();
}
