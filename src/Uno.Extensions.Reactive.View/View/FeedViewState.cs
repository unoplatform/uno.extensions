using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive;

[Windows.UI.Xaml.Data.Bindable]
public sealed class FeedViewState : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	private readonly Dictionary<string, object?> _values = new();

	private object? _parent;

	private object? _data;
	private Exception? _error;
	private bool _isTransient;

	internal FeedViewState()
	{
	}

	internal void Update(IMessage message)
	{
		foreach (var change in message.Changes)
		{
			if (message.Current[change] is { IsSet: true } value)
			{
				_values[change.Identifier] = value.Value;
			}
			else
			{
				_values.Remove(change.Identifier);
			}

			OnPropertyChanged($"Item[{change.Identifier}]");
		}
		
		Data = message.Current.Data.SomeOrDefault();
		Error = message.Current.Error;
		IsTransient = message.Current.IsTransient;
	}

	/// <summary>
	/// Gets the parent DataContext
	/// </summary>
	public object? Parent
	{
		get => _parent;
		internal set
		{
			_parent = value;
			OnPropertyChanged();
		}
	}

	public object? Data
	{
		get => _data;
		private set
		{
			_data = value;
			OnPropertyChanged();
		}
	}

	public Exception? Error
	{
		get => _error;
		private set
		{
			_error = value;
			OnPropertyChanged();
		}
	}

	public bool IsTransient
	{
		get => _isTransient;
		private set
		{
			_isTransient = value;
			OnPropertyChanged();
		}
	}

	public object? this[string axis] => _values.TryGetValue(axis, out var value) ? value : default;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
