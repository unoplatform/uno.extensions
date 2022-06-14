using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Uno.Extensions.Reactive.Testing;

public abstract class ChangesConstraint : Constraint<ChangeCollection>
{
}
