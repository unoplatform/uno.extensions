using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

public interface ICommandBuilder
{
	public ICommandBuilder<T> Given<T>(IFeed<T> parameter);

	public void Then(AsyncAction execute);

	public void Execute(AsyncAction execute);

	// public void Then(Signal execute);
}
