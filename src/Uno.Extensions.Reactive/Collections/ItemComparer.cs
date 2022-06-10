using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.Collections;

/// <summary>
/// A set of comparers to track items in a list.
/// </summary>
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
/// If they are considered as equals by the <paramref name="Entity" /> comparer, they are compared using teh <see cref="Version"/> comparer.
/// If **not** equals (i.e. 2 **distinct version** of the **same entity**, a replace collection changed args is generated,
/// so the binding engine kicks-in to update the properties of the item.
/// If equals (i.e. 2 **instances** of the **same version** of the **same entity** (all properties are equals) no event is raised at all.
/// </remarks>
/// <remarks>
///	For both comparer, prefer to provide `null` than <see cref="EqualityComparer{T}.Default"/>.
/// This allow user of this struct to use fast-paths when possible.
/// </remarks>
internal record struct ItemComparer(IEqualityComparer? Entity, IEqualityComparer? Version);
