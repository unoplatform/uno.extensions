using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// A collection of <see cref="RefreshToken"/>.
/// </summary>
/// <param name="Versions">The tokens.</param>
internal record RefreshTokenCollection(IImmutableList<RefreshToken> Versions)
{
	/// <summary>
	/// An empty collection.
	/// </summary>
	public static RefreshTokenCollection Empty { get; } = new(ImmutableList<RefreshToken>.Empty);

	/// <summary>
	/// Aggregates multiple token collections to keep only the highest version of each source.
	/// </summary>
	/// <param name="versions">The </param>
	/// <returns>A new collection containing only the highest version of each source.</returns>
	public static RefreshTokenCollection Aggregate(IReadOnlyCollection<RefreshTokenCollection> versions)
		=> new(versions
			.SelectMany(set => set.Versions)
			.GroupBy(version => (version.Source, version.RootContextId))
			.Select(group => group.OrderBy(request => request.Version).Last())
			.ToImmutableList());

	/// <summary>
	/// Indicates if this collection contains at least one token.
	/// </summary>
	public bool IsEmpty => Versions.Count == 0;

	public bool IsLower(IMessage message)
		=> IsLower(message.Current.Get(MessageAxis.Refresh) ?? Empty);

	public bool IsLower(RefreshTokenCollection actual)
	{
		var joined = Versions
			.Join(
				actual.Versions,
				v => (v.Source, v.RootContextId),
				v => (v.Source, v.RootContextId),
				(e, a) => a.Version < e.Version)
			.ToList();

		return joined.Count == Versions.Count && joined.All(isGreaterOrEquals => isGreaterOrEquals);
	}

	public bool IsGreaterOrEquals(IMessage message)
		=> IsGreaterOrEquals(message.Current.Get(MessageAxis.Refresh) ?? Empty);

	public bool IsGreaterOrEquals(RefreshTokenCollection actual)
	{
		var joined = Versions
			.Join(
				actual.Versions,
				v => (v.Source, v.RootContextId),
				v => (v.Source, v.RootContextId),
				(e, a) => a.Version >= e.Version)
			.ToList();

		return joined.Count == Versions.Count && joined.All(isGreaterOrEquals => isGreaterOrEquals);
	}

	public static implicit operator RefreshTokenCollection(RefreshToken version)
		=> new(ImmutableList.Create(version));

	/// <inheritdoc />
	public override string ToString()
		=> string.Join(",", Versions);
}
