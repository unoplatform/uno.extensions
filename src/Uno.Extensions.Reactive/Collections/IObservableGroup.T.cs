using System;
using System.Linq;

namespace Uno.Extensions.Collections;

/// <summary>
/// A group of <typeparamref name="T"/> which notifies read and write oprations.
/// </summary>
/// <typeparam name="T">Type of the items</typeparam>
internal interface IObservableGroup<T> : IObservableGroup, IObservableCollection<T>
{
}
