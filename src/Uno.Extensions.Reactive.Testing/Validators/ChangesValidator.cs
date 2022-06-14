using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Extensions.Reactive.Testing;

public class ChangesValidator<T> : Constraint<ChangeCollection>
{
	private readonly IImmutableList<ChangesConstraint> _constraints;

	public ChangesValidator(ChangedConstraint<T>[] constraints)
		=> _constraints = constraints.Select(c => c.Value).ToImmutableList();

	public ChangesValidator(params ChangesConstraint[] constraints)
		=> _constraints = constraints.ToImmutableList();

	public ChangesValidator(IImmutableList<ChangesConstraint> constraints)
		=> _constraints = constraints;

	/// <inheritdoc />
	public override void Assert(ChangeCollection actual)
	{
		foreach (var constraint in _constraints)
		{
			constraint.Assert(actual);
		}
	}
}
