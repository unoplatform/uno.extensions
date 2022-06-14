using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Testing;

public abstract class Constraint<TSubject> // This is not an interface to allow implicit operators on it
{
	public abstract void Assert(TSubject actual);
}
