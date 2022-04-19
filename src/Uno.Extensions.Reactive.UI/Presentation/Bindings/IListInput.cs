using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IListInput<T> : IListState<T>
{
	/// <summary>
	/// The name of bindable property.
	/// </summary>
	public string PropertyName { get; }
}
