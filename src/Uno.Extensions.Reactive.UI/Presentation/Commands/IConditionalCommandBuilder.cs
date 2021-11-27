using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

public interface IConditionalCommandBuilder<out T>
{
	public void Then(AsyncAction<T> execute);

	// public void Then(Signal execute);
}
