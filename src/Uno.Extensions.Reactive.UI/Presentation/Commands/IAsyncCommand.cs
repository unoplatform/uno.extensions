using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Uno.Extensions.Reactive;

/// <summary>
/// An asynchronous <see cref="ICommand"/>.
/// </summary>
public interface IAsyncCommand : ICommand, INotifyPropertyChanged
{
	/// <summary>
	/// Indicates if the command is currently running.
	/// </summary>
	public bool IsExecuting { get; }
}
