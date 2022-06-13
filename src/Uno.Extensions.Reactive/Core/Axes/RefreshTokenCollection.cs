using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Uno.Extensions.Reactive.Core.FeedHelper;

namespace Uno.Extensions.Reactive.Sources;

///// <summary>
///// A collection of <see cref="RefreshToken"/>.
///// </summary>
///// <param name="Tokens">The tokens.</param>
//internal record RefreshTokenCollection(IImmutableList<RefreshToken> Tokens) : TokenCollection<RefreshToken>(Tokens)
//{
//	/// <summary>
//	/// Creates a new collection with a single item.
//	/// </summary>
//	public static implicit operator RefreshTokenCollection(RefreshToken version)
//		=> new(ImmutableList.Create(version));
//}

internal interface IToken
{
	object Source { get; }

	uint RootContextId { get; }

	uint SequenceId { get; }
}

internal interface IToken<out TToken> : IToken
{
	TToken Next();
}

/// <summary>
/// A collection of <see cref="IToken"/>.
/// </summary>
/// <param name="Tokens">The tokens.</param>
internal record TokenCollection<TToken>(IImmutableList<TToken> Tokens)
	where TToken : IToken
{
	/// <summary>
	/// An empty collection.
	/// </summary>
	public static TokenCollection<TToken> Empty { get; } = new(ImmutableList<TToken>.Empty);

	/// <summary>
	/// Aggregates multiple token collections to keep only the highest version of each source.
	/// </summary>
	/// <param name="versions">The </param>
	/// <returns>A new collection containing only the highest version of each source.</returns>
	public static TokenCollection<TToken> Aggregate(IEnumerable<TokenCollection<TToken>> versions)
		=> new(versions
			.SelectMany(set => set.Tokens)
			.GroupBy(version => (version.Source, version.RootContextId))
			.Select(group => group.OrderBy(request => request.SequenceId).Last())
			.ToImmutableList());

	/// <summary>
	/// Indicates if this collection contains at least one token.
	/// </summary>
	public bool IsEmpty => Tokens.Count == 0;

	/// <summary>
	/// Check if the version defined in this collection are all lower than versions defined in the given message
	/// </summary>
	/// <param name="message">The message from which version should be compared to.</param>
	/// <param name="axis">The axis of the token collection.</param>
	/// <returns>'true' if message contains a version greater or equals to the version contained by this collection, 'false' otherwise.</returns>
	public bool IsLower(IMessage message, MessageAxis<TokenCollection<TToken>> axis)
		=> IsLower(message.Current.Get(axis) ?? Empty);

	/// <summary>
	/// Check if the version defined in this collection are all lower than versions defined in the given collection
	/// </summary>
	/// <param name="actual">The collection from which version should be compared to.</param>
	/// <returns>'true' if given collection contains a version greater or equals to the version contained by this collection, 'false' otherwise.</returns>
	public bool IsLower(TokenCollection<TToken> actual)
	{
		var joined = Tokens
			.Join(
				actual.Tokens,
				v => (v.Source, v.RootContextId),
				v => (v.Source, v.RootContextId),
				(e, a) => a.SequenceId < e.SequenceId)
			.ToList();

		return joined.Count == Tokens.Count && joined.All(isGreaterOrEquals => isGreaterOrEquals);
	}

	/// <summary>
	/// Check if the version defined in this collection are all greater or equals than versions defined in the given message
	/// </summary>
	/// <param name="message">The message from which version should be compared to.</param>
	/// <param name="axis">The axis of the token collection.</param>
	/// <returns>'true' if message contains a lower version to the version contained by this collection, 'false' otherwise.</returns>
	public bool IsGreaterOrEquals(IMessage message, MessageAxis<TokenCollection<TToken>> axis)
		=> IsGreaterOrEquals(message.Current.Get(axis) ?? Empty);

	/// <summary>
	/// Check if the version defined in this collection are all greater or equals than versions defined in the given collection
	/// </summary>
	/// <param name="actual">The collection from which version should be compared to.</param>
	/// <returns>'true' if given collection contains a lower version to the version contained by this collection, 'false' otherwise.</returns>
	public bool IsGreaterOrEquals(TokenCollection<TToken> actual)
	{
		var joined = Tokens
			.Join(
				actual.Tokens,
				v => (v.Source, v.RootContextId),
				v => (v.Source, v.RootContextId),
				(e, a) => a.SequenceId >= e.SequenceId)
			.ToList();

		return joined.Count == Tokens.Count && joined.All(isGreaterOrEquals => isGreaterOrEquals);
	}

	/// <summary>
	/// Creates a new collection with a single item.
	/// </summary>
	public static implicit operator TokenCollection<TToken>(TToken token)
		=> new(ImmutableList.Create(token));

	/// <inheritdoc />
	public override string ToString()
		=> string.Join(",", Tokens.Select(t => $"[ctx{t.RootContextId}] {GetDebugIdentifier(t.Source)} v{t.SequenceId}"));
}
