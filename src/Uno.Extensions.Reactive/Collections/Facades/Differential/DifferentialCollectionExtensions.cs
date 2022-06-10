using System;
using System.Linq;

namespace Uno.Extensions.Collections.Facades.Differential;

internal static class DifferentialCollectionExtensions
{
	/// <summary>
	/// Tries to gets the common ancestor node shared with another <see cref="IDifferentialCollection"/>, if any.
	/// Cf. remark for perf considerations
	/// </summary>
	/// <param name="collection">The source collection.</param>
	/// <param name="other">The other collection to check.</param>
	/// <returns>The shared common ancestor if any.</returns>
	/// <remarks>
	/// For perf consideration, if a collection B is just the evolution a collection A (like `B = A.Add(something)`),
	/// it's better to do A.FindCommonAncestor(B).
	/// </remarks>
	public static IDifferentialCollectionNode? FindCommonAncestor(this IDifferentialCollection collection, IDifferentialCollection other)
	{
		var node = collection.Head;
		while (node is not null)
		{
			var otherNode = other.Head;
			while (otherNode is not null)
			{
				if (node == otherNode)
				{
					return node;
				}

				otherNode = otherNode.Previous;
			}

			node = node.Previous;
		}

		return default;
	}
}
