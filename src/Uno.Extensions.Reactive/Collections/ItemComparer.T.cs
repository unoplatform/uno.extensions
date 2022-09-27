using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Equality;

namespace Uno.Extensions.Reactive.Collections;

/// <summary>
/// A set of comparers to track items in a list.
/// </summary>
/// <typeparam name="T">Type of the items in the list.</typeparam>
/// <param name="Entity">Comparer used to detect multiple versions of the **same entity (T)**, or null to use default.</param>
/// <param name="Version">Comparer used to detect multiple instance of the **same version** of the **same entity (T)**, or null to rely only on the <paramref name="Entity"/> (not recommended).</param>
/// <remarks>
/// Usually the <paramref name="Entity"/> should only compare the ID of the entities in order to properly track the changes made on an entity.
/// <br />
/// On the other side the <paramref name="Version"/> is used to determine if two instances of the same entity (which was considered as equals by the <paramref name="Entity"/>)
/// are effectively equals or not (i.e. same version or not).
/// So it's expected that it does a deep comparison of all properties of the 2 version of a same item.
/// <br />
/// The typical usage of this structure is for item tracking in collections.
/// We first try to match the items using the <paramref name="Entity"/> comparer to detect insertions and removes.
/// When 2 items are consider as not equals, we generate some add / remove collection changed args.
/// If they are considered as equals by the <paramref name="Entity" /> comparer, they are compared using the <paramref name="Version"/> comparer.
/// If **not** equals (i.e. 2 **distinct version** of the **same entity**), a replace collection changed args is generated,
/// so the binding engine kicks-in to update the properties of the item.
/// If equals (i.e. 2 **instances** of the **same version** of the **same entity**), all properties are equals no event is raised at all.
/// </remarks>
/// <remarks>
/// The common usages are: <br />
///	* If the <typeparamref name="T"/> is an immutable record, with a notion of key
///   (so a new instance of the record, with the same key, is created on each update, and a new immutable collection is created using that new instance)
///   use the <see cref="KeyEqualityComparer{T}"/> as <paramref name="Entity"/> and the <see cref="EqualityComparer{T}.Default"/> for <paramref name="Version"/>.
/// * For immutable records that does not have a notion of key / version,
///   use the <see cref="EqualityComparer{T}.Default"/> as <paramref name="Entity"/> and keep the for <paramref name="Version"/> `null`.
/// * For an entity that as no notion of key (only relies on instance equality),
///   use `null` for both <paramref name="Entity"/> and <paramref name="Version"/>.
/// <br />
/// The general rules are:
/// * The <paramref name="Version"/> should be more restrictive than the <paramref name="Entity"/> comparer.
/// * If there is no notion of key/version for your items, the <paramref name="Version"/> should be `null` (rely only on <paramref name="Entity"/> for tracking).
/// * If the <paramref name="Entity"/> is `null` (lets to the list the responsibility to choose the right comparer,
///   so usually ref-equals for classes, deep-equals for records and ValueTypes), the <paramref name="Version"/> should also be `null`.
/// * Do not use the same comparer for both
/// * Prefer to keep <paramref name="Entity"/> `null` than <see cref="EqualityComparer{T}.Default"/>, it allows user of this struct to use fast-paths when possible.
///   (so the <paramref name="Version"/> should also be null then).
/// </remarks>
internal record struct ItemComparer<T>(IEqualityComparer<T>? Entity, IEqualityComparer<T>? Version)
{
	public static ItemComparer<T> Null => new();

	public static ItemComparer<T> Default => new(null, null);

	// A bool which indicates that we went trough a constructor instead of using default(ItemComparer<T>) (both comparers can still be null)
	public bool IsSet { get; init; } = true;
	public bool IsNull => !IsSet;

	public static explicit operator ItemComparer(ItemComparer<T> typed)
		=> new(typed.Entity?.ToEqualityComparer(), typed.Version?.ToEqualityComparer()) { IsSet = typed.IsSet };

	public static explicit operator ItemComparer<T>(ItemComparer untyped)
		=> new(untyped.Entity?.ToEqualityComparer<T>(), untyped.Version?.ToEqualityComparer<T>()) { IsSet = untyped.IsSet };
}
