using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Expose the current state of a <see cref="FeedView"/> for template's bindings.
/// </summary>
[Bindable]
public sealed class FeedViewState : System.ComponentModel.INotifyPropertyChanged
{
	/// <inheritdoc />
	public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

	private readonly Dictionary<string, object?> _values = new();

	private object? _parent;

	private object? _data;
	private Exception? _error;
	private bool _progress;

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
		Progress = message.Current.IsTransient;
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

	/// <summary>
	/// The value reported by the last message received from the source feed.
	/// </summary>
	public object? Data
	{
		get => _data;
		private set
		{
			_data = value;
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// The error reported by the last message received from the source feed.
	/// </summary>
	public Exception? Error
	{
		get => _error;
		private set
		{
			_error = value;
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// The progress reported by the last message received from the source feed.
	/// </summary>
	public bool Progress
	{
		get => _progress;
		private set
		{
			_progress = value;
			OnPropertyChanged();
		}
	}

	/// <summary>
	/// Gets the last metadata of a given axis identifier received from the source feed.
	/// </summary>
	/// <param name="axis">The <see cref="MessageAxis.Identifier"/> of the axis to get.</param>
	/// <returns>The value of the metadata.</returns>
	public object? this[string axis] => _values.TryGetValue(axis, out var value) ? value : default;

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
}
