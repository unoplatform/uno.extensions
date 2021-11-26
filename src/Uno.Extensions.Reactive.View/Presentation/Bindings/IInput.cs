using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

public interface IInput<T> : IState<T>
{
	public string PropertyName { get; }
}
