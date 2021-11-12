using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Testing;

public abstract class ConstraintPart<TConstrained> // This is not an interface to allow implicit operators on it
{
	public abstract void Assert(TConstrained actual);
}
