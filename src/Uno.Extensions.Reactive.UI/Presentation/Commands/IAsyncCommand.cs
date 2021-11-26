using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace Uno.Extensions.Reactive;

public interface IAsyncCommand : ICommand, INotifyPropertyChanged
{
	public bool IsExecuting { get; }
}
