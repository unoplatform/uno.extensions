using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.Extensions.Reactive;

public class Bindable<T> : IBindable, INotifyPropertyChanged
{
	/// <inheritdoc />
	public event PropertyChangedEventHandler? PropertyChanged;

	private T? _value;
	private CancellationTokenSource? _asyncSetCt;
	private List<Action<T?>>? _onUpdated;

	private readonly BindablePropertyInfo<T> _property;

	public Bindable(BindablePropertyInfo<T> property)
	{
		if (!property.IsValid)
		{
			throw new ArgumentException("Property IsInvalid, it must be created from a BindableViewModel or a Bindable", nameof(property));
		}

		_property = property;

		property.Subscribe(OnBackingStateUpdated);
	}

	protected BindablePropertyInfo<TProperty> Property<TProperty>(
		string propertyName,
		Func<T?, TProperty?> get,
		Func<T?, TProperty?, T> set)
		=> new(
			this,
			propertyName,
			updated =>
			{
				(_onUpdated ??= new()).Add(value => updated(get(value)));
				updated(get(_value)); // Make sure to propagate the default value
			},
			async (update, ct) => await _property.Update(t => set(t, update(get(t))), ct));

	public T? GetValue()
		=> _value;

	public void SetValue(T? value)
	{
		if (SetValueCore(value))
		{
			UpdateBackingState(value);
		}
	}

	private bool SetValueCore(T? value)
	{
		if (object.ReferenceEquals(_value, value))
		{
			return false;
		}

		_value = value;

		if (_onUpdated is {} callbacks)
		{
			foreach (var callback in callbacks)
			{
				callback(value);
			}
		}

		_property.NotifyUpdated();

		return true;
	}

	private void OnBackingStateUpdated(T? value)
	{
		SetValueCore(value);
	}

	private async void UpdateBackingState(T? value)
	{
		try
		{
			_asyncSetCt?.Cancel();
			_asyncSetCt = new();

			await Task.Run(() => _property.Update(_ => value, _asyncSetCt.Token), _asyncSetCt.Token);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception error)
		{
			this.Log().Error(
				$"Synchronization from View to ViewModel of '{_property}' failed. "
				+ "(This is a temporary error, it will be retried on next change from the View.)",
				error);
		}
	}

	void IBindable.OnPropertyChanged(string propertyName)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
