using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

public interface ICommandBuilder<out T>
{
	public IConditionalCommandBuilder<T> When(Predicate<T> canExecute);

	public void Then(ActionAsync<T> execute);

	//public void Then(Signal execute);
}
