using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Defines a metadata axis of a <see cref="MessageEntry{T}"/>
/// </summary>
public abstract class MessageAxis : IEquatable<MessageAxis>
{
	private readonly int _hashCode;

	/// <summary>
	/// The <see cref="MessageAxis"/> of the <see cref="MessageEntry{T}.Data"/>.
	/// </summary>
	/// <remarks>
	/// The Data axis a special axis that must be set on all MessageEntry. To keep it "unset", use the Option.Undefined.
	/// </remarks>
	public static DataAxis Data => DataAxis.Instance;

	/// <summary>
	/// The <see cref="MessageAxis"/> of the <see cref="MessageEntry{T}.Error"/>.
	/// </summary>
	public static MessageAxis<Exception> Error { get; } = new(MessageAxes.Error, FeedHelper.AggregateErrors);

	/// <summary>
	/// The <see cref="MessageAxis"/> of the <see cref="MessageEntry{T}.IsTransient"/>.
	/// </summary>
	public static ProgressAxis Progress => ProgressAxis.Instance;

	/// <summary>
	/// For a refreshable source, this axis contains information about the version of this source.
	/// </summary>
	/// <remarks>
	/// This is expected to be full-filled only by "source" feed that are refreshable,
	/// not the sources feed built from a stream of data nor operators.
	/// </remarks>
	internal static MessageAxis<TokenSet<RefreshToken>> Refresh => RefreshAxis.Instance;

	/// <summary>
	/// For a refreshable source, this axis contains information about the version of this source.
	/// </summary>
	/// <remarks>
	/// This is expected to be full-filled only by "source" feed that are refreshable,
	/// not the sources feed built from a stream of data nor operators.
	/// </remarks>
	internal static MessageAxis<PaginationInfo> Pagination => new(MessageAxes.Pagination, PaginationInfo.Aggregate);

	/// <summary>
	/// For a refreshable source, this axis contains information about the version of this source.
	/// </summary>
	/// <remarks>
	/// This is expected to be full-filled only by "source" feed that are refreshable,
	/// not the sources feed built from a stream of data nor operators.
	/// </remarks>
	internal static MessageAxis<SelectionInfo> Selection => new(MessageAxes.Selection, SelectionInfo.Aggregate);

	internal MessageAxis(string identifier)
	{
		Identifier = identifier;

		_hashCode = identifier.GetHashCode();
	}

	/// <summary>
	/// The unique identifier of the axis.
	/// </summary>
	public string Identifier { get; }

	internal bool IsTransient { get; init; }

	[Pure]
	internal virtual (MessageAxisValue values, IChangeSet? changes) GetLocalValue(MessageAxisValue parent, MessageAxisValue currentLocal, (MessageAxisValue value, IChangeSet? changes) updatedLocal)
	{
		if (!parent.IsSet)
		{
			return updatedLocal;
		}
		else if (!updatedLocal.value.IsSet)
		{
			return (parent, null);
		}
		else
		{
			return (Aggregate(new[] { parent, updatedLocal.value }), null);
		}
	}

	/// <summary>
	/// Aggregates multiple raw axis values of the axis into a single one.
	/// </summary>
	/// <param name="values">The values to aggregate.</param>
	/// <returns>A raw axis value that may contains an aggregation of the provided values, the most important one, or any other combination.</returns>
	protected internal abstract MessageAxisValue Aggregate(IEnumerable<MessageAxisValue> values);

	/// <summary>
	/// Determines if 2 raw axis values are equals.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>True if values should be considered as equals, False otherwise.</returns>
	protected internal abstract bool AreEquals(MessageAxisValue left, MessageAxisValue right);

	/// <inheritdoc />
	[Pure]
	public override int GetHashCode()
		=> _hashCode;

	/// <inheritdoc />
	[Pure]
	public bool Equals(MessageAxis? other)
		=> other is not null && Equals(this, other);

	/// <inheritdoc />
	[Pure]
	public override bool Equals(object? obj)
		=> obj is MessageAxis other && Equals(this, other);

	[Pure]
	private static bool Equals(MessageAxis left, MessageAxis right)
		=> left.Identifier.Equals(right.Identifier);

	/// <summary>
	/// Determines if 2 raw axis values are equals.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>True if values should be considered as equals, False otherwise.</returns>
	[Pure]
	public static bool operator ==(MessageAxis left, MessageAxis right)
		=> Equals(left, right);

	/// <summary>
	/// Determines if 2 raw axis values not are equals.
	/// </summary>
	/// <param name="left">The first value to compare.</param>
	/// <param name="right">The second value to compare.</param>
	/// <returns>True if values should be considered as not equals, False otherwise.</returns>
	[Pure]
	public static bool operator !=(MessageAxis left, MessageAxis right)
		=> !Equals(left, right);

	/// <inheritdoc />
	[Pure]
	public override string ToString()
		=> IsTransient
			? Identifier + "~"
			: Identifier;
}
