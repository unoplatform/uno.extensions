using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Uno.Toolkit;

namespace Uno.Extensions.Reactive;

/// <summary>
/// An asynchronous <see cref="ICommand"/>.
/// </summary>
public interface IAsyncCommand : ICommand, INotifyPropertyChanged, ILoadable
{
}
