using System;
using System.Collections;
using System.Linq;

namespace Uno.Extensions.Equality;

/// <summary>
/// A provider that can be used in the <see cref="KeyEqualityComparer"/> registry.
/// </summary>
/// <remarks>This should not be implemented by application. It's expected to be generated the KeyEqualityGenerationTool.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IKeyEqualityProvider
{
	/// <summary>
	/// Tries to get a key equality comparer for the given type.
	/// </summary>
	/// <param name="type">The type of the object to compare.</param>
	/// <returns>A key equality comparer if this provider is able to create one for the given type, null otherwise.</returns>
	/// <remarks>This should not be used in application's code, you should instead use the <see cref="KeyEqualityComparer.Find{T}"/> method.</remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	IEqualityComparer? TryGet(Type type);
}
