using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Uno.Extensions.Reactive;

public abstract class MessageAxis : IEquatable<MessageAxis>
{
	private readonly int _hashCode;

	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// The Data axis a special axis that must be set on all MessageEntry. To keep it "unset", use the Option.Undefined.
	/// </remarks>
	public static DataAxis Data => DataAxis.Instance;

	public static MessageAxis<Exception> Error { get; } = new(MessageAxises.Error, FeedHelper.AggregateErrors);

	public static ProgressAxis Progress => ProgressAxis.Instance;

	internal MessageAxis(string identifier)
	{
		Identifier = identifier;

		_hashCode = identifier.GetHashCode();
	}

	public string Identifier { get; }

	[Pure]
	internal virtual MessageAxisValue GetLocalValue(MessageAxisValue parent, MessageAxisValue local)
	{
		if (!parent.IsSet)
		{
			return local;
		}
		else if (!local.IsSet)
		{
			return parent;
		}
		else
		{
			return Aggregate(new[] { parent, local });
		}
	}

	protected internal abstract MessageAxisValue Aggregate(IEnumerable<MessageAxisValue> values);

	protected internal abstract bool AreEquals(MessageAxisValue left, MessageAxisValue right);

	/// <inheritdoc />
	[Pure]
	public override int GetHashCode()
		=> _hashCode;

	/// <inheritdoc />
	[Pure]
	public bool Equals(MessageAxis other)
		=> Equals(this, other);

	/// <inheritdoc />
	[Pure]
	public override bool Equals(object obj)
		=> obj is MessageAxis other && Equals(this, other);

	[Pure]
	private static bool Equals(MessageAxis left, MessageAxis right)
		=> left.Identifier.Equals(right.Identifier);

	[Pure]
	public static bool operator ==(MessageAxis left, MessageAxis right)
		=> Equals(left, right);

	[Pure]
	public static bool operator !=(MessageAxis left, MessageAxis right)
		=> !Equals(left, right);
}
