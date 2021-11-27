using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

public interface ICommandBuilder<out T>
{
	public IConditionalCommandBuilder<T> When(Predicate<T> canExecute);

	public void Then(AsyncAction<T> execute);

	public void Execute(AsyncAction<T> execute);

	//public void Then(Signal execute);
}
