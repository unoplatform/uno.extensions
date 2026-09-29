using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// The <see cref="MessageAxis"/> of the <see cref="MessageEntry{T}.Validation"/>.
/// </summary>
/// <remarks>
/// Values of this axis are local to the feed that set them: they are not forwarded to feeds derived from it (Select, Combine, etc.).
/// </remarks>
internal sealed class ValidationAxis : MessageAxis<IImmutableList<ValidationResult>>
{
	internal static ValidationAxis Instance { get; } = new();

	private ValidationAxis()
		: base(MessageAxes.Validation, Concat)
	{
		IsLocal = true;
	}

	/// <inheritdoc />
	[Pure]
	public override MessageAxisValue ToMessageValue(IImmutableList<ValidationResult>? value)
		=> value is null or { Count: 0 } ? MessageAxisValue.Unset : new(value);

	/// <inheritdoc />
	[Pure]
	internal override (MessageAxisValue values, IChangeSet? changes) GetLocalValue(MessageAxisValue parent, MessageAxisValue currentLocal, (MessageAxisValue value, IChangeSet? changes) updatedLocal)
		=> updatedLocal; // Results describe a given version of the data: a local value replaces the parent one, they are never merged.

	/// <inheritdoc />
	[Pure]
	protected internal override bool AreEquals(MessageAxisValue left, MessageAxisValue right)
		=> left.IsSet == right.IsSet
			&& (!left.IsSet || AreEquals(FromMessageValue(left), FromMessageValue(right)));

	[Pure]
	internal static bool AreEquals(IReadOnlyList<ValidationResult>? left, IReadOnlyList<ValidationResult>? right)
	{
		if (ReferenceEquals(left, right))
		{
			return true;
		}

		var leftCount = left?.Count ?? 0;
		if (leftCount != (right?.Count ?? 0))
		{
			return false;
		}

		for (var i = 0; i < leftCount; i++)
		{
			if (!AreEquals(left![i], right![i]))
			{
				return false;
			}
		}

		return true;
	}

	[Pure]
	internal static bool AreEquals(ValidationResult left, ValidationResult right)
		=> ReferenceEquals(left, right)
			|| (string.Equals(left.ErrorMessage, right.ErrorMessage, StringComparison.Ordinal)
				&& left.MemberNames.SequenceEqual(right.MemberNames, StringComparer.Ordinal));

	private static IImmutableList<ValidationResult> Concat(IReadOnlyCollection<IImmutableList<ValidationResult>> values)
		=> values.SelectMany(results => results).ToImmutableList();
}
