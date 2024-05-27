using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Collections;
using Uno.Extensions.Reactive.Tests._Utils;
using Uno.Extensions.Reactive.Utils;
using static Uno.Extensions.Collections.CollectionChanged;

namespace Uno.Extensions.Reactive.Tests.Collections.Tracking;

[TestClass]
public partial class Given_CollectionAnalyzer_Immutable
{
	private IEqualityComparer<MyClass> ItemComparer { get; } = FuncEqualityComparer<MyClass>.Create(c => c.Value);

	[TestMethod]
	public void When_Add_ValueType()
	{
		FromInt(0, 2)
			.To(0, 1, 2)
			.With(null, null)
			.ShouldBe(
				Add(1, 1)
			);
	}

	[TestMethod]
	public void When_AddFirst_ValueType()
	{
		FromInt(1, 2)
			.To(0, 1, 2)
			.With(null, null)
			.ShouldBe(
				Add(0, 0)
			);
	}

	[TestMethod]
	public void When_AddFirstSome_ValueType()
	{
		FromInt(2, 3)
			.To(0, 1, 2, 3)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {0, 1}, 0)
			);
	}

	[TestMethod]
	public void When_AddLast_ValueType()
	{
		FromInt(0, 1)
			.To(0, 1, 2)
			.With(null, null)
			.ShouldBe(
				Add(2, 2)
			);
	}

	[TestMethod]
	public void When_AddLastSome_ValueType()
	{
		FromInt(0, 1)
			.To(0, 1, 2, 3, 4)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {2, 3, 4}, 2)
			);
	}

	[TestMethod]
	public void When_AddLastSomeAndMove_ValueType()
	{
		FromInt(0, 1, 4)
			.To(0, 1, 2, 3, 4, 5, 6)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {2, 3}, 2),
				AddSome(new[] {5, 6}, 5)
			);
	}

	[TestMethod]
	public void When_AddDuplicatedSome_ValueType()
	{
		FromInt(0, 1, 2, 3, 4)
			.To(0, 2, 3, 1, 2, 3, 4)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3}, 2, 1),
				AddSome(new[] {2, 3}, 4)
			);
	}

	[TestMethod]
	public void When_AddDuplicatedFirstSome_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(1, 2, 0, 1, 2, 3)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {1, 2}, 1, 0),
				AddSome(new[] {1, 2}, 3)
			);
	}

	[TestMethod]
	public void When_AddDuplicatedLastSome_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(0, 1, 2, 3, 0, 1, 2, 3, 2)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {0, 1, 2, 3, 2}, 4)
			);
	}

	[TestMethod]
	public void When_Move_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(0, 2, 1, 3)
			.With(null, null)
			.ShouldBe(
				Move(2, 2, 1)
			);
	}

	[TestMethod]
	public void When_MoveSome_ValueType()
	{
		FromInt(0, 1, 2, 3, 4)
			.To(0, 2, 3, 1, 4)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3}, 2, 1)
			);
	}

	[TestMethod]
	public void When_MoveFirst_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(1, 2, 0, 3)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {1, 2}, 1, 0)
			);
	}

	[TestMethod]
	public void When_MoveFirstSome_ValueType()
	{
		FromInt(0, 1, 2, 3, 4)
			.To(2, 3, 0, 1, 4)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3}, 2, 0)
			);
	}

	[TestMethod]
	public void When_MoveLast_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(0, 3, 1, 2)
			.With(null, null)
			.ShouldBe(
				Move(3, 3, 1)
			);
	}

	[TestMethod]
	public void When_MoveLastSome_ValueType()
	{
		FromInt(0, 1, 2, 3, 4)
			.To(0, 3, 4, 1, 2)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {3, 4}, 3, 1)
			);
	}

	[TestMethod]
	public void When_MoveDuplicatedSome_ValueType()
	{
		/* ix:	0  1  2  3  4  5  6  7
		*
		* src: 0  1  2 (3) 1  2  3  4
		*		0 [3] 1  2  1  2  3 (4)
		*		0  3 [4] 1  2  1  2 (3)
		*		0  3  4  1  2 [3] 1  2
		*/

		FromInt(0, 1, 2, 3, 1, 2, 3, 4)
			.To(0, 3, 4, 1, 2, 3, 1, 2)
			.With(null, null)
			.ShouldBe(
				Move(3, 3, 1),
				Move(4, 7, 2),
				Move(3, 7, 5)
			);
	}

	[TestMethod]
	public void When_MoveDuplicatedFirstSome_ValueType()
	{
		FromInt(0, 1, 0, 1, 2, 3)
			.To(2, 0, 1, 0, 1, 3)
			.With(null, null)
			.ShouldBe(
				Move(2, 4, 0)
			);
	}

	[TestMethod]
	public void When_MoveDuplicatedLastSome_ValueType()
	{
		FromInt(0, 1, 2, 3, 2, 3)
			.To(0, 2, 3, 2, 3, 1)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3, 2, 3}, 2, 1)
			);
	}

	[TestMethod]
	public void When_Remove_ValueType()
	{
		FromInt(0, 1, 2)
			.To(0, 2)
			.With(null, null)
			.ShouldBe(
				Remove(1, 1)
			);
	}

	[TestMethod]
	public void When_RemoveFirst_ValueType()
	{
		FromInt(0, 1, 2)
			.To(1, 2)
			.With(null, null)
			.ShouldBe(
				Remove(0, 0)
			);
	}

	[TestMethod]
	public void When_RemoveFirstSome_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(2, 3)
			.With(null, null)
			.ShouldBe(
				RemoveSome(new[] {0, 1}, 0)
			);
	}

	[TestMethod]
	public void When_RemoveLast_ValueType()
	{
		FromInt(0, 1, 2)
			.To(0, 1)
			.With(null, null)
			.ShouldBe(
				Remove(2, 2)
			);
	}

	[TestMethod]
	public void When_RemoveLastSome_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(0, 1)
			.With(null, null)
			.ShouldBe(
				RemoveSome(new[] {2, 3}, 2)
			);
	}

	[TestMethod]
	public void When_RemoveDuplicatedLastSome_ValueType()
	{
		FromInt(0, 1, 2, 3, 0, 1, 2, 3, 2)
			.To(0, 1, 2, 3)
			.With(null, null)
			.ShouldBe(
				RemoveSome(new[] {0, 1, 2, 3, 2}, 4)
			);
	}

	[TestMethod]
	public void When_Add_ReferenceType()
	{
		FromObj(0, 2)
			.To(0, 1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 2}, new[] {0, 2}, 0),
				Add(1, 1)
			);
	}

	[TestMethod]
	public void When_AddSome_ReferenceType()
	{
		FromObj(0, 3)
			.To(0, 1, 2, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 3}, new[] {0, 3}, 0),
				AddSome(new[] {1, 2}, 1)
			);
	}

	[TestMethod]
	public void When_AddNonConsecutiveSome_ReferenceType()
	{
		FromObj(0, 2, 4)
			.To(0, 1, 2, 3, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 2, 4}, new[] {0, 2, 4}, 0),
				Add(1, 1),
				Add(3, 3)
			);
	}

	[TestMethod]
	public void When_AddFirst_ReferenceType()
	{
		FromObj(1, 2)
			.To(0, 1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {1, 2}, new[] {1, 2}, 0),
				Add(0, 0)
			);
	}

	[TestMethod]
	public void When_AddFirstSome_ReferenceType()
	{
		FromObj(2, 3)
			.To(0, 1, 2, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {2, 3}, new[] {2, 3}, 0),
				AddSome(new[] {0, 1}, 0)
			);
	}

	[TestMethod]
	public void When_AddFirstNonConsecutiveSome_ReferenceType()
	{
		FromObj(1, 2, 4)
			.To(0, 1, 2, 3, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {1, 2, 4}, new[] {1, 2, 4}, 0),
				Add(0, 0),
				Add(3, 3)
			);
	}

	[TestMethod]
	public void When_AddLast_ReferenceType()
	{
		FromObj(0, 1)
			.To(0, 1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1}, new[] {0, 1}, 0),
				Add(2, 2)
			);
	}

	[TestMethod]
	public void When_AddLastSome_ReferenceType()
	{
		FromObj(0, 1)
			.To(0, 1, 2, 3, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1}, new[] {0, 1}, 0),
				AddSome(new[] {2, 3, 4}, 2)
			);
	}

	[TestMethod]
	public void When_AddLastNonConsecutiveSome_ReferenceType()
	{
		FromObj(0, 2, 3)
			.To(0, 1, 2, 3, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 2, 3}, new[] {0, 2, 3}, 0),
				Add(1, 1),
				Add(4, 4)
			);
	}

	[TestMethod]
	public void When_AddLastSomeAndMove_ReferenceType()
	{
		FromObj(0, 1, 4)
			.To(0, 1, 2, 3, 4, 5, 6)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 4}, new[] {0, 1, 4}, 0),
				AddSome(new[] {2, 3}, 2),
				AddSome(new[] {5, 6}, 5)
			);
	}

	[TestMethod]
	public void When_AddDuplicatedSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 2, 3, 1, 2, 3, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3, 4}, new[] {0, 1, 2, 3, 4}, 0),
				MoveSome(new[] {2, 3}, 2, 1),
				AddSome(new[] {2, 3}, 4)
			);
	}

	[TestMethod]
	public void When_AddDuplicatedFirstSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(1, 2, 0, 1, 2, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3}, new[] {0, 1, 2, 3}, 0),
				MoveSome(new[] {1, 2}, 1, 0),
				AddSome(new[] {1, 2}, 3)
			);
	}

	[TestMethod]
	public void When_AddDuplicatedLastSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1, 2, 3, 0, 1, 2, 3, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3}, new[] {0, 1, 2, 3}, 0),
				AddSome(new[] {0, 1, 2, 3, 2}, 4)
			);
	}

	[TestMethod]
	public void When_Move_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 2, 1, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3}, new[] {0, 1, 2, 3}, 0),
				Move(2, 2, 1)
			);
	}

	[TestMethod]
	public void When_MoveSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 2, 3, 1, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3, 4}, new[] {0, 1, 2, 3, 4}, 0),
				MoveSome(new[] {2, 3}, 2, 1)
			);
	}

	[TestMethod]
	public void When_MoveFirst_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(1, 2, 0, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3}, new[] {0, 1, 2, 3}, 0),
				MoveSome(new[] {1, 2}, 1, 0)
			);
	}

	[TestMethod]
	public void When_MoveFirstSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(2, 3, 0, 1, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3, 4}, new[] {0, 1, 2, 3, 4}, 0),
				MoveSome(new[] {2, 3}, 2, 0)
			);
	}

	[TestMethod]
	public void When_MoveLast_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 3, 1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3}, new[] {0, 1, 2, 3}, 0),
				Move(3, 3, 1)
			);
	}

	[TestMethod]
	public void When_MoveLastSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 3, 4, 1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3, 4}, new[] {0, 1, 2, 3, 4}, 0),
				MoveSome(new[] {3, 4}, 3, 1)
			);
	}

	[TestMethod]
	public void When_MoveDuplicatedSome_ReferenceType()
	{
		/* ix:	0  1  2  3  4  5  6  7
		*
		* src: 0  1  2 (3) 1  2  3  4
		*		0 [3] 1  2  1  2  3 (4)
		*		0  3 [4] 1  2  1  2 (3)
		*		0  3  4  1  2 [3] 1  2
		*/

		FromObj(0, 1, 2, 3, 1, 2, 3, 4)
			.To(0, 3, 4, 1, 2, 3, 1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3, 1, 2, 3, 4}, new[] {0, 1, 2, 3, 1, 2, 3, 4}, 0),
				Move(3, 3, 1),
				Move(4, 7, 2),
				Move(3, 7, 5)
			);
	}

	[TestMethod]
	public void When_MoveDuplicatedFirstSome_ReferenceType()
	{
		FromObj(0, 1, 0, 1, 2, 3)
			.To(2, 0, 1, 0, 1, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 0, 1, 2, 3}, new[] {0, 1, 0, 1, 2, 3}, 0),
				Move(2, 4, 0)
			);
	}

	[TestMethod]
	public void When_MoveDuplicatedLastSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 2, 3)
			.To(0, 2, 3, 2, 3, 1)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3, 2, 3}, new[] {0, 1, 2, 3, 2, 3}, 0),
				MoveSome(new[] {2, 3, 2, 3}, 2, 1)
			);
	}

	[TestMethod]
	public void When_Remove_ReferenceType()
	{
		FromObj(0, 1, 2)
			.To(0, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				Replace(0, 0, 0),
				Replace(2, 2, 2),
				Remove(1, 1)
			);
	}

	[TestMethod]
	public void When_RemoveSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				Replace(0, 0, 0),
				Replace(3, 3, 3),
				RemoveSome(new[] {1, 2}, 1)
			);
	}

	[TestMethod]
	public void When_RemoveNonConsecutiveSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 2, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				Replace(0, 0, 0),
				Replace(2, 2, 2),
				Replace(4, 4, 4),
				Remove(1, 1),
				Remove(3, 2)
			);
	}

	[TestMethod]
	public void When_RemoveFirst_ReferenceType()
	{
		FromObj(0, 1, 2)
			.To(1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {1, 2}, new[] {1, 2}, 1),
				Remove(0, 0)
			);
	}

	[TestMethod]
	public void When_RemoveFirstSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(2, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {2, 3}, new[] {2, 3}, 2),
				RemoveSome(new[] {0, 1}, 0)
			);
	}

	[TestMethod]
	public void When_RemoveFirstNonConsecutiveSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(1, 2, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {1, 2}, new[] {1, 2}, 1),
				Replace(4, 4, 4),
				Remove(0, 0),
				Remove(3, 2)
			);
	}

	[TestMethod]
	public void When_RemoveLast_ReferenceType()
	{
		FromObj(0, 1, 2)
			.To(0, 1)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1}, new[] {0, 1}, 0),
				Remove(2, 2)
			);
	}

	[TestMethod]
	public void When_RemoveLastSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1}, new[] {0, 1}, 0),
				RemoveSome(new[] {2, 3}, 2)
			);
	}

	[TestMethod]
	public void When_RemoveLastNonConsecutiveSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 2, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				Replace(0, 0, 0),
				ReplaceSome(new[] {2, 3}, new[] {2, 3}, 2),
				Remove(1, 1),
				Remove(4, 3)
			);
	}

	[TestMethod]
	public void When_RemoveDuplicatedLastSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 0, 1, 2, 3, 2)
			.To(0, 1, 2, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3}, new[] {0, 1, 2, 3}, 0),
				RemoveSome(new[] {0, 1, 2, 3, 2}, 4)
			);
	}

	[TestMethod]
	public void When_UpdateFirst_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To((0, 1), 1, 2, 3)
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				Replace((0, 0), (0, 1), 0)
			);
	}

	[TestMethod]
	public void When_UpdateFirstSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To((0, 1), (1, 1), 2, 3)
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1}, new[] {(0, 1), (1, 1)}, 0)
			);
	}


	[TestMethod]
	public void When_Update_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, (1, 1), 2, 3)
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				Replace(1, (1, 1), 1)
			);
	}

	[TestMethod]
	public void When_UpdateSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, (1, 1), (2, 1), 3)
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {1, 2}, new[] {(1, 1), (2, 1)}, 1)
			);
	}

	[TestMethod]
	public void When_UpdateLast_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1, 2, (3, 1))
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				Replace(3, (3, 1), 3)
			);
	}

	[TestMethod]
	public void When_UpdateLastSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1, (2, 1), (3, 1))
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {2, 3}, new[] {(2, 1), (3, 1)}, 2)
			);
	}

	[TestMethod]
	public void When_Add_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 2)
			.To(0, 1, 2)
			.With(null, null)
			.ShouldBe(
				Add(1, 1)
			);
	}

	[TestMethod]
	public void When_AddFirst_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(1, 2)
			.To(0, 1, 2)
			.With(null, null)
			.ShouldBe(
				Add(0, 0)
			);
	}

	[TestMethod]
	public void When_AddFirstSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(2, 3)
			.To(0, 1, 2, 3)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {0, 1}, 0)
			);
	}

	[TestMethod]
	public void When_AddLast_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1)
			.To(0, 1, 2)
			.With(null, null)
			.ShouldBe(
				Add(2, 2)
			);
	}

	[TestMethod]
	public void When_AddLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1)
			.To(0, 1, 2, 3, 4)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {2, 3, 4}, 2)
			);
	}

	[TestMethod]
	public void When_AddLastSomeAndMove_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 4)
			.To(0, 1, 2, 3, 4, 5, 6)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {2, 3}, 2),
				AddSome(new[] {5, 6}, 5)
			);
	}

	[TestMethod]
	public void When_AddDuplicatedSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 2, 3, 1, 2, 3, 4)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3}, 2, 1),
				AddSome(new[] {2, 3}, 4)
			);
	}

	[TestMethod]
	public void When_AddDuplicatedFirstSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(1, 2, 0, 1, 2, 3)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {1, 2}, 1, 0),
				AddSome(new[] {1, 2}, 3)
			);
	}

	[TestMethod]
	public void When_AddDuplicatedLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1, 2, 3, 0, 1, 2, 3, 2)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {0, 1, 2, 3, 2}, 4)
			);
	}

	[TestMethod]
	public void When_Move_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 2, 1, 3)
			.With(null, null)
			.ShouldBe(
				Move(2, 2, 1)
			);
	}

	[TestMethod]
	public void When_MoveSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 2, 3, 1, 4)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3}, 2, 1)
			);
	}

	[TestMethod]
	public void When_MoveFirst_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(1, 2, 0, 3)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {1, 2}, 1, 0)
			);
	}

	[TestMethod]
	public void When_MoveFirstSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(2, 3, 0, 1, 4)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3}, 2, 0)
			);
	}

	[TestMethod]
	public void When_MoveLast_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 3, 1, 2)
			.With(null, null)
			.ShouldBe(
				Move(3, 3, 1)
			);
	}

	[TestMethod]
	public void When_MoveLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 3, 4, 1, 2)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {3, 4}, 3, 1)
			);
	}

	[TestMethod]
	public void When_MoveDuplicatedSome_ReferenceType_WithoutRefrencesUpdates()
	{
		/* ix:	0  1  2  3  4  5  6  7
		*
		* src: 0  1  2 (3) 1  2  3  4
		*		0 [3] 1  2  1  2  3 (4)
		*		0  3 [4] 1  2  1  2 (3)
		*		0  3  4  1  2 [3] 1  2
		*/

		FromObj(0, 1, 2, 3, 1, 2, 3, 4)
			.To(0, 3, 4, 1, 2, 3, 1, 2)
			.With(null, null)
			.ShouldBe(
				Move(3, 3, 1),
				Move(4, 7, 2),
				Move(3, 7, 5)
			);
	}

	[TestMethod]
	public void When_MoveDuplicatedFirstSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 0, 1, 2, 3)
			.To(2, 0, 1, 0, 1, 3)
			.With(null, null)
			.ShouldBe(
				Move(2, 4, 0)
			);
	}

	[TestMethod]
	public void When_MoveDuplicatedLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3, 2, 3)
			.To(0, 2, 3, 2, 3, 1)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3, 2, 3}, 2, 1)
			);
	}

	[TestMethod]
	public void When_Remove_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2)
			.To(0, 2)
			.With(null, null)
			.ShouldBe(
				Remove(1, 1)
			);
	}

	[TestMethod]
	public void When_RemoveFirst_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2)
			.To(1, 2)
			.With(null, null)
			.ShouldBe(
				Remove(0, 0)
			);
	}

	[TestMethod]
	public void When_RemoveFirstSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(2, 3)
			.With(null, null)
			.ShouldBe(
				RemoveSome(new[] {0, 1}, 0)
			);
	}

	[TestMethod]
	public void When_RemoveLast_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2)
			.To(0, 1)
			.With(null, null)
			.ShouldBe(
				Remove(2, 2)
			);
	}

	[TestMethod]
	public void When_RemoveLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1)
			.With(null, null)
			.ShouldBe(
				RemoveSome(new[] {2, 3}, 2)
			);
	}

	[TestMethod]
	public void When_RemoveDuplicatedLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3, 0, 1, 2, 3, 2)
			.To(0, 1, 2, 3)
			.With(null, null)
			.ShouldBe(
				RemoveSome(new[] {0, 1, 2, 3, 2}, 4)
			);
	}

	[TestMethod]
	public void When_UpdateFirst_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To((0, 1), 1, 2, 3)
			.With(ItemComparer, null)
			.ShouldBeEmpty();
	}

	[TestMethod]
	public void When_UpdateFirstSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To((0, 1), (1, 1), 2, 3)
			.With(ItemComparer, null)
			.ShouldBeEmpty();
	}


	[TestMethod]
	public void When_Update_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, (1, 1), 2, 3)
			.With(ItemComparer, null)
			.ShouldBeEmpty();
	}

	[TestMethod]
	public void When_UpdateSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, (1, 1), (2, 1), 3)
			.With(ItemComparer, null)
			.ShouldBeEmpty();
	}

	[TestMethod]
	public void When_UpdateLast_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1, 2, (3, 1))
			.With(ItemComparer, null)
			.ShouldBeEmpty();
	}

	[TestMethod]
	public void When_UpdateLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1, (2, 1), (3, 1))
			.With(ItemComparer, null)
			.ShouldBeEmpty();
	}

	[TestMethod]
	public void When_WithVisitor_Add_ValueType()
	{
		FromInt(0, 2)
			.To(0, 1, 2)
			.With()
			.ShouldBe(
				Add(1, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddFirst_ValueType()
	{
		FromInt(1, 2)
			.To(0, 1, 2)
			.With(null, null)
			.ShouldBe(
				Add(0, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddFirstSome_ValueType()
	{
		FromInt(2, 3)
			.To(0, 1, 2, 3)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {0, 1}, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddLast_ValueType()
	{
		FromInt(0, 1)
			.To(0, 1, 2)
			.With(null, null)
			.ShouldBe(
				Add(2, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddLastSome_ValueType()
	{
		FromInt(0, 1)
			.To(0, 1, 2, 3, 4)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {2, 3, 4}, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddLastSomeAndMove_ValueType()
	{
		FromInt(0, 1, 4)
			.To(0, 1, 2, 3, 4, 5, 6)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {2, 3}, 2),
				AddSome(new[] {5, 6}, 5)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddDuplicatedSome_ValueType()
	{
		FromInt(0, 1, 2, 3, 4)
			.To(0, 2, 3, 1, 2, 3, 4)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3}, 2, 1),
				AddSome(new[] {2, 3}, 4)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddDuplicatedFirstSome_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(1, 2, 0, 1, 2, 3)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {1, 2}, 1, 0),
				AddSome(new[] {1, 2}, 3)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddDuplicatedLastSome_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(0, 1, 2, 3, 0, 1, 2, 3, 2)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {0, 1, 2, 3, 2}, 4)
			);
	}

	[TestMethod]
	public void When_WithVisitor_Move_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(0, 2, 1, 3)
			.With(null, null)
			.ShouldBe(
				Move(2, 2, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveSome_ValueType()
	{
		FromInt(0, 1, 2, 3, 4)
			.To(0, 2, 3, 1, 4)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3}, 2, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveFirst_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(1, 2, 0, 3)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {1, 2}, 1, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveFirstSome_ValueType()
	{
		FromInt(0, 1, 2, 3, 4)
			.To(2, 3, 0, 1, 4)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3}, 2, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveLast_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(0, 3, 1, 2)
			.With(null, null)
			.ShouldBe(
				Move(3, 3, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveLastSome_ValueType()
	{
		FromInt(0, 1, 2, 3, 4)
			.To(0, 3, 4, 1, 2)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {3, 4}, 3, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveDuplicatedSome_ValueType()
	{
		/* ix:	0  1  2  3  4  5  6  7
		*
		* src: 0  1  2 (3) 1  2  3  4
		*		0 [3] 1  2  1  2  3 (4)
		*		0  3 [4] 1  2  1  2 (3)
		*		0  3  4  1  2 [3] 1  2
		*/

		FromInt(0, 1, 2, 3, 1, 2, 3, 4)
			.To(0, 3, 4, 1, 2, 3, 1, 2)
			.With(null, null)
			.ShouldBe(
				Move(3, 3, 1),
				Move(4, 7, 2),
				Move(3, 7, 5)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveDuplicatedFirstSome_ValueType()
	{
		FromInt(0, 1, 0, 1, 2, 3)
			.To(2, 0, 1, 0, 1, 3)
			.With(null, null)
			.ShouldBe(
				Move(2, 4, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveDuplicatedLastSome_ValueType()
	{
		FromInt(0, 1, 2, 3, 2, 3)
			.To(0, 2, 3, 2, 3, 1)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3, 2, 3}, 2, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_Remove_ValueType()
	{
		FromInt(0, 1, 2)
			.To(0, 2)
			.With(null, null)
			.ShouldBe(
				Remove(1, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveFirst_ValueType()
	{
		FromInt(0, 1, 2)
			.To(1, 2)
			.With(null, null)
			.ShouldBe(
				Remove(0, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveFirstSome_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(2, 3)
			.With(null, null)
			.ShouldBe(
				RemoveSome(new[] {0, 1}, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveLast_ValueType()
	{
		FromInt(0, 1, 2)
			.To(0, 1)
			.With(null, null)
			.ShouldBe(
				Remove(2, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveLastSome_ValueType()
	{
		FromInt(0, 1, 2, 3)
			.To(0, 1)
			.With(null, null)
			.ShouldBe(
				RemoveSome(new[] {2, 3}, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveDuplicatedLastSome_ValueType()
	{
		FromInt(0, 1, 2, 3, 0, 1, 2, 3, 2)
			.To(0, 1, 2, 3)
			.With(null, null)
			.ShouldBe(
				RemoveSome(new[] {0, 1, 2, 3, 2}, 4)
			);
	}

	[TestMethod]
	public void When_WithVisitor_Add_ReferenceType()
	{
		FromObj(0, 2)
			.To(0, 1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 2}, new[] {0, 2}, 0),
				Add(1, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddFirst_ReferenceType()
	{
		FromObj(1, 2)
			.To(0, 1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {1, 2}, new[] {1, 2}, 0),
				Add(0, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddFirstSome_ReferenceType()
	{
		FromObj(2, 3)
			.To(0, 1, 2, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {2, 3}, new[] {2, 3}, 0),
				AddSome(new[] {0, 1}, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddLast_ReferenceType()
	{
		FromObj(0, 1)
			.To(0, 1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1}, new[] {0, 1}, 0),
				Add(2, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddLastSome_ReferenceType()
	{
		FromObj(0, 1)
			.To(0, 1, 2, 3, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1}, new[] {0, 1}, 0),
				AddSome(new[] {2, 3, 4}, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddLastSomeAndMove_ReferenceType()
	{
		FromObj(0, 1, 4)
			.To(0, 1, 2, 3, 4, 5, 6)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 4}, new[] {0, 1, 4}, 0),
				AddSome(new[] {2, 3}, 2),
				AddSome(new[] {5, 6}, 5)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddDuplicatedSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 2, 3, 1, 2, 3, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3, 4}, new[] {0, 1, 2, 3, 4}, 0),
				MoveSome(new[] {2, 3}, 2, 1),
				AddSome(new[] {2, 3}, 4)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddDuplicatedFirstSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(1, 2, 0, 1, 2, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3}, new[] {0, 1, 2, 3}, 0),
				MoveSome(new[] {1, 2}, 1, 0),
				AddSome(new[] {1, 2}, 3)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddDuplicatedLastSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1, 2, 3, 0, 1, 2, 3, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3}, new[] {0, 1, 2, 3}, 0),
				AddSome(new[] {0, 1, 2, 3, 2}, 4)
			);
	}

	[TestMethod]
	public void When_WithVisitor_Move_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 2, 1, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] { 0, 1, 2, 3 }, new[] { 0, 1, 2, 3 }, 0),
				Move(2, 2, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 2, 3, 1, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3, 4}, new[] { 0, 1, 2, 3, 4 }, 0),
				MoveSome(new[] {2, 3}, 2, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveFirst_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(1, 2, 0, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3}, new[] {0, 1, 2, 3}, 0),
				MoveSome(new[] {1, 2}, 1, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveFirstSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(2, 3, 0, 1, 4)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3, 4}, new[] { 0, 1, 2, 3, 4 }, 0),
				MoveSome(new[] {2, 3}, 2, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveLast_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 3, 1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] { 0, 1, 2, 3 }, new[] { 0, 1, 2, 3 }, 0),
				Move(3, 3, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveLastSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 3, 4, 1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] { 0, 1, 2, 3, 4 }, new[] { 0, 1, 2, 3, 4 }, 0),
				MoveSome(new[] {3, 4}, 3, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveDuplicatedSome_ReferenceType()
	{
		/* ix:	0  1  2  3  4  5  6  7
		*
		* src: 0  1  2 (3) 1  2  3  4
		*		0 [3] 1  2  1  2  3 (4)
		*		0  3 [4] 1  2  1  2 (3)
		*		0  3  4  1  2 [3] 1  2
		*/

		FromObj(0, 1, 2, 3, 1, 2, 3, 4)
			.To(0, 3, 4, 1, 2, 3, 1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] { 0, 1, 2, 3, 1, 2, 3, 4 }, new[] { 0, 1, 2, 3, 1, 2, 3, 4 }, 0),
				Move(3, 3, 1),
				Move(4, 7, 2),
				Move(3, 7, 5)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveDuplicatedFirstSome_ReferenceType()
	{
		FromObj(0, 1, 0, 1, 2, 3)
			.To(2, 0, 1, 0, 1, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 0, 1, 2, 3}, new[] {0, 1, 0, 1, 2, 3}, 0),
				Move(2, 4, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveDuplicatedLastSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 2, 3)
			.To(0, 2, 3, 2, 3, 1)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3, 2, 3}, new[] {0, 1, 2, 3, 2, 3}, 0),
				MoveSome(new[] {2, 3, 2, 3}, 2, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_Remove_ReferenceType()
	{
		FromObj(0, 1, 2)
			.To(0, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				Replace(0, 0, 0),
				Replace(2, 2, 2),
				Remove(1, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveFirst_ReferenceType()
	{
		FromObj(0, 1, 2)
			.To(1, 2)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] { 1, 2 }, new[] { 1, 2 }, 1),
				Remove(0, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveFirstSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(2, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] { 2, 3 }, new[] { 2, 3 }, 2),
				RemoveSome(new[] {0, 1}, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveLast_ReferenceType()
	{
		FromObj(0, 1, 2)
			.To(0, 1)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1}, new[] {0, 1}, 0),
				Remove(2, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveLastSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1}, new[] {0, 1}, 0),
				RemoveSome(new[] {2, 3}, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveDuplicatedLastSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3, 0, 1, 2, 3, 2)
			.To(0, 1, 2, 3)
			.With(null, ReferenceEqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3}, new[] {0, 1, 2, 3}, 0),
				RemoveSome(new[] {0, 1, 2, 3, 2}, 4)
			);
	}

	[TestMethod]
	public void When_WithVisitor_UpdateFirst_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To((0, 1), 1, 2, 3)
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				Replace((0, 0), (0, 1), 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_UpdateFirstSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To((0, 1), (1, 1), 2, 3)
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1}, new[] {(0, 1), (1, 1)}, 0)
			);
	}


	[TestMethod]
	public void When_WithVisitor_Update_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, (1, 1), 2, 3)
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				Replace(1, (1, 1), 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_UpdateSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, (1, 1), (2, 1), 3)
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {1, 2}, new[] {(1, 1), (2, 1)}, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_UpdateLast_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1, 2, (3, 1))
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				Replace(3, (3, 1), 3)
			);
	}

	[TestMethod]
	public void When_WithVisitor_UpdateLastSome_ReferenceType()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1, (2, 1), (3, 1))
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {2, 3}, new[] {(2, 1), (3, 1)}, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_Add_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 2)
			.To(0, 1, 2)
			.With(null, null)
			.ShouldBe(
				Add(1, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddFirst_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(1, 2)
			.To(0, 1, 2)
			.With(null, null)
			.ShouldBe(
				Add(0, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddFirstSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(2, 3)
			.To(0, 1, 2, 3)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {0, 1}, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddLast_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1)
			.To(0, 1, 2)
			.With(null, null)
			.ShouldBe(
				Add(2, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1)
			.To(0, 1, 2, 3, 4)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {2, 3, 4}, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddLastSomeAndMove_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 4)
			.To(0, 1, 2, 3, 4, 5, 6)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {2, 3}, 2),
				AddSome(new[] {5, 6}, 5)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddDuplicatedSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 2, 3, 1, 2, 3, 4)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3}, 2, 1),
				AddSome(new[] {2, 3}, 4)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddDuplicatedFirstSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(1, 2, 0, 1, 2, 3)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {1, 2}, 1, 0),
				AddSome(new[] {1, 2}, 3)
			);
	}

	[TestMethod]
	public void When_WithVisitor_AddDuplicatedLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1, 2, 3, 0, 1, 2, 3, 2)
			.With(null, null)
			.ShouldBe(
				AddSome(new[] {0, 1, 2, 3, 2}, 4)
			);
	}

	[TestMethod]
	public void When_WithVisitor_Move_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 2, 1, 3)
			.With(null, null)
			.ShouldBe(
				Move(2, 2, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 2, 3, 1, 4)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3}, 2, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveFirst_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(1, 2, 0, 3)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {1, 2}, 1, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveFirstSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(2, 3, 0, 1, 4)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3}, 2, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveLast_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 3, 1, 2)
			.With(null, null)
			.ShouldBe(
				Move(3, 3, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 3, 4, 1, 2)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {3, 4}, 3, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveDuplicatedSome_ReferenceType_WithoutRefrencesUpdates()
	{
		/* ix:	0  1  2  3  4  5  6  7
		*
		* src: 0  1  2 (3) 1  2  3  4
		*		0 [3] 1  2  1  2  3 (4)
		*		0  3 [4] 1  2  1  2 (3)
		*		0  3  4  1  2 [3] 1  2
		*/

		FromObj(0, 1, 2, 3, 1, 2, 3, 4)
			.To(0, 3, 4, 1, 2, 3, 1, 2)
			.With(null, null)
			.ShouldBe(
				Move(3, 3, 1),
				Move(4, 7, 2),
				Move(3, 7, 5)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveDuplicatedFirstSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 0, 1, 2, 3)
			.To(2, 0, 1, 0, 1, 3)
			.With(null, null)
			.ShouldBe(
				Move(2, 4, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_MoveDuplicatedLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3, 2, 3)
			.To(0, 2, 3, 2, 3, 1)
			.With(null, null)
			.ShouldBe(
				MoveSome(new[] {2, 3, 2, 3}, 2, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_Remove_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2)
			.To(0, 2)
			.With(null, null)
			.ShouldBe(
				Remove(1, 1)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveFirst_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2)
			.To(1, 2)
			.With(null, null)
			.ShouldBe(
				Remove(0, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveFirstSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(2, 3)
			.With(null, null)
			.ShouldBe(
				RemoveSome(new[] {0, 1}, 0)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveLast_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2)
			.To(0, 1)
			.With(null, null)
			.ShouldBe(
				Remove(2, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1)
			.With(null, null)
			.ShouldBe(
				RemoveSome(new[] {2, 3}, 2)
			);
	}

	[TestMethod]
	public void When_WithVisitor_RemoveDuplicatedLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3, 0, 1, 2, 3, 2)
			.To(0, 1, 2, 3)
			.With(null, null)
			.ShouldBe(
				RemoveSome(new[] {0, 1, 2, 3, 2}, 4)
			);
	}

	[TestMethod]
	public void When_WithVisitor_UpdateFirst_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To((0, 1), 1, 2, 3)
			.With(ItemComparer, null)
			.ShouldBe();
	}

	[TestMethod]
	public void When_WithVisitor_UpdateFirstSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To((0, 1), (1, 1), 2, 3)
			.With(ItemComparer, null)
			.ShouldBe();
	}


	[TestMethod]
	public void When_WithVisitor_Update_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, (1, 1), 2, 3)
			.With(ItemComparer, null)
			.ShouldBe();
	}

	[TestMethod]
	public void When_WithVisitor_UpdateSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, (1, 1), (2, 1), 3)
			.With(ItemComparer, null)
			.ShouldBe();
	}

	[TestMethod]
	public void When_WithVisitor_UpdateLast_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1, 2, (3, 1))
			.With(ItemComparer, null)
			.ShouldBe();
	}

	[TestMethod]
	public void When_WithVisitor_UpdateLastSome_ReferenceType_WithoutRefrencesUpdates()
	{
		FromObj(0, 1, 2, 3)
			.To(0, 1, (2, 1), (3, 1))
			.With(ItemComparer, null)
			.ShouldBe();
	}

	[TestMethod]
	public void Scenario_1()
	{
		FromObj(0, 1, 2, 3, 4)
			.To(0, 2)
			.With(ItemComparer, null)
			.ShouldBe(
				Remove(1, 1),
				RemoveSome(new[] {3, 4}, 2)
			);
	}

	[TestMethod]
	public void Scenario_2()
	{
		FromObj(0, 1, 2, 3, 4, 5, 6, 7)
			.To(10, 11, 2, 13, 14, 0, 1, 3, 4, 5)
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				AddSome(new[] {10, 11}, 0),
				Move(2, 4, 2),
				AddSome(new[] {13, 14}, 3),
				RemoveSome(new[] {6, 7}, 10)
			);
	}

	[TestMethod]
	public void Scenario_2_WithUpdates()
	{
		FromObj(0, 1, 2, 3, 4, 5, 6, 7)
			.To(10, 11, (2, 1), 13, 14, (0, 1), (1, 1), (3, 1), (4, 1), (5, 1))
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] { 0, 1, 2, 3, 4, 5 }, new[] { (0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (5, 1) }, 0),
				AddSome(new[] {10, 11}, 0),
				Move((2, 1), 4, 2),
				AddSome(new[] {13, 14}, 3),
				RemoveSome(new[] {6, 7}, 10)
			);
	}


	[TestMethod]
	public void Scenario_3_MoveMultipleWithUpdates()
	{
		FromObj(0, 1, 2, 3, 4, 5)
			.To(6, (1, 1), (2, 1), (3, 1), (5, 1), (0, 1), (4, 1))
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				ReplaceSome(new[] {0, 1, 2, 3, 4, 5}, new[] {(0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (5, 1)}, 0),
				Add(6, 0),
				MoveSome(new[] {(1, 1), (2, 1), (3, 1)}, 2, 1),
				Move((5, 1), 6, 4)
			);
	}

	[TestMethod]
	public void Scenario_4_ReOrder()
	{
		FromObj(0, 1, 2, 3, 4, 5)
			.To(5, 4, 3, 2, 1, 0)
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				Move(5, 5, 0),
				Move(4, 5, 1),
				Move(3, 5, 2),
				Move(2, 5, 3),
				Move(1, 5, 4)
			);
	}

	[TestMethod]
	public void Scenario_4_ReOrderWithMixedMoves()
	{
		FromObj(0, 1, 2, 3, 4, 5, 42)
			.To(42, 0, 1, 5, 4, 3, 2)
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				Move(42, 6, 0),
				Move(5, 6, 3),
				Move(4, 6, 4),
				Move(3, 6, 5)
			);
	}

	[TestMethod]
	public void Scenario_4_ReOrderWithMixedMoves2()
	{
		/*
		*	 0   1   2   3   4   5   6
		*
		*	 0   1  (42) 2   3   4   5
		*  [42] 0   1   2   3   4  (5)
		*   42 [5]  0   1   2   3  (4)
		*   42  5  [4]  0   1   2  (3)
		*   42  5   4  [3]  0   1  (2)
		*   42  5   4   3  [2]  0  (1)
		*   42  5   4   3   2  [1]  0
		*/


		FromObj(0, 1, 42, 2, 3, 4, 5)
			.To(42, 5, 4, 3, 2, 1, 0)
			.With(ItemComparer, EqualityComparer<MyClass>.Default)
			.ShouldBe(
				Move(42, 2, 0),
				Move(5, 6, 1),
				Move(4, 6, 2),
				Move(3, 6, 3),
				Move(2, 6, 4),
				Move(1, 6, 5)
			);
	}

	private static ImmutableListCollectionTrackerTester<MyClass> FromObj(params MyClass[] items)
		=> new(ImmutableList.Create(items), null);

	private static ImmutableListCollectionTrackerTester<int> FromInt(params int[] items)
		=> new(ImmutableList.Create(items), null);

	internal class ImmutableListCollectionTrackerTester<T> : CollectionTrackerTester<IImmutableList<T>, T>
	{
		/// <inheritdoc />
		public ImmutableListCollectionTrackerTester(IImmutableList<T> previous, IImmutableList<T>? updated)
			: base(previous, updated)
		{
		}

		/// <inheritdoc />
		protected override IImmutableList<T> Create(T[] items)
			=> ImmutableList.Create(items);

		/// <inheritdoc />
		protected override IEnumerable<T> AsEnumerable(IImmutableList<T> collection)
			=> collection;

		/// <inheritdoc />
		protected override CollectionUpdater GetUpdater(ItemComparer<T> comparer, IImmutableList<T> previous, IImmutableList<T> updated, ICollectionUpdaterVisitor visitor)
			=> new CollectionAnalyzer<T>(comparer).GetUpdater(previous, updated, visitor);

		/// <inheritdoc />
		protected override CollectionChangeSet<T> GetChanges(ItemComparer<T> comparer, IImmutableList<T> previous, IImmutableList<T> updated)
			=> new CollectionAnalyzer<T>(comparer).GetChanges(previous, updated);
	}
}
