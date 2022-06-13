using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Data;
using Umbrella.Feeds.Tests._TestUtils;
using Umbrella.Presentation.Feeds.Tests.Collections._TestUtils;
using Uno.Extensions.Collections;
using Uno.Extensions.Reactive.Bindings.Collections;
using Uno.Extensions.Reactive.Tests;
using Uno.Extensions.Reactive.Utils;

namespace Umbrella.Presentation.Feeds.Tests.Collections
{
	[TestClass]
	public class Given_BindableCollection : FeedUITests
	{
		[TestMethod]
		public void When_Flat_InsertItem_EmptyCollection()
		{
			OnFlatList(0)
				.Do
				(
					l => l.Add(new MyItem(42))
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemInserted, 0) & new[] {new MyItem(42)}
				);
		}

		[TestMethod]
		public void When_Flat_InsertFirstItem()
		{
			OnFlatList(5)
				.Do
				(
					l => l.Insert(0, new MyItem(42))
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemInserted, 0) & (Count) 6 & (0, new MyItem(42))
				);
		}

		[TestMethod]
		public void When_Flat_InsertItem()
		{
			OnFlatList(5)
				.Do
				(
					l => l.Insert(2, new MyItem(42))
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemInserted, 2) & (Count) 6 & (2, new MyItem(42))
				);
		}

		[TestMethod]
		public void When_Flat_InsertLastItem()
		{
			OnFlatList(5)
				.Do
				(
					l => l.Add(new MyItem(42))
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemInserted, 5) & (Count) 6 & (5, new MyItem(42))
				);
		}

		[TestMethod]
		public void When_Flat_UpdateFirstItem()
		{
			OnFlatList(5)
				.Do
				(
					l => l[0] = l[0].Update()
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemChanged, 0) & (Count) 5 & (0, i => i.Version == 1)
				);
		}

		[TestMethod]
		public void When_Flat_UpdateItem()
		{
			OnFlatList(5)
				.Do
				(
					l => l[2] = l[2].Update()
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemChanged, 2) & (Count) 5 & (2, i => i.Version == 1)
				);
		}

		[TestMethod]
		public void When_Flat_UpdateLastItem()
		{
			OnFlatList(5)
				.Do
				(
					l => l[4] = l[4].Update()
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemChanged, 4) & (Count) 5 & (4, i => i.Version == 1)
				);
		}

		[TestMethod]
		public void When_Flat_ReplaceFirstItem()
		{
			OnFlatList(5)
				.Do
				(
					l => l[0] = new MyItem(42)
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 4,
					e => e & (CollectionChange.ItemInserted, 0) & (Count) 5 & (0, new MyItem(42))
				);
		}

		[TestMethod]
		public void When_Flat_ReplaceItem()
		{
			OnFlatList(5)
				.Do
				(
					l => l[2] = new MyItem(42)
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemRemoved, 2) & (Count) 4,
					e => e & (CollectionChange.ItemInserted, 2) & (Count) 5 & (2, new MyItem(42))
				);
		}

		[TestMethod]
		public void When_Flat_ReplaceLastItem()
		{
			OnFlatList(5)
				.Do
				(
					l => l[4] = new MyItem(42)
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemRemoved, 4) & (Count) 4,
					e => e & (CollectionChange.ItemInserted, 4) & (Count) 5 & (4, new MyItem(42))
				);
		}

		[TestMethod]
		public void When_Flat_With_Current_ReplaceItem()
		{
			var list = OnFlatList(5);
			var collectionView = list.View as ICollectionView;
			bool runOnce = false;

			void Before()
			{
				collectionView!.MoveCurrentToPosition(2);
				Assert.AreEqual(2, collectionView.CurrentPosition);

				collectionView.VectorChanged += (o, e) =>
				{
					if (e.CollectionChange == CollectionChange.ItemChanged)
					{
						if (e.Index == collectionView.CurrentPosition)
						{
							// Simulate behaviour of UI
							collectionView.MoveCurrentTo(null);
							runOnce = true;
							Assert.AreEqual(-1, collectionView.CurrentPosition);
						}
					}
				};
			}

			list.Do
				(
					_ => Before(),
					l => l[2] = new MyItem(l[2].Id, l[2].Version + 1),
					_ => After()
				);

			void After()
			{
				Assert.IsTrue(runOnce);
				Assert.AreEqual(2, collectionView!.CurrentPosition);
			}
		}

		[TestMethod]
		public void When_Flat_RemoveFirstItem()
		{
			OnFlatList()
				.Do
				(
					l => l.Remove(l[0])
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 4
				);
		}

		[TestMethod]
		public void When_Flat_RemoveItem()
		{
			OnFlatList()
				.Do
				(
					l => l.Remove(l[2])
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemRemoved, 2) & (Count) 4
				);
		}

		[TestMethod]
		public void When_Flat_RemoveLastItem()
		{
			OnFlatList(5)
				.Do
				(
					l => l.Remove(l[4])
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemRemoved, 4) & (Count) 4
				);
		}

		[TestMethod]
		public void When_Flat_RemoveItem_EmptyCollection()
		{
			OnFlatList(1)
				.Do
				(
					l => l.Remove(l.Single())
				)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 0
				);
		}

		[TestMethod]
		public void When_Flat_Clear()
		{
			OnFlatList(5)
				.Do
				(
					l => l.Clear()
				)
				.ShouldBe
				(
					e => e & (CollectionChange.Reset, 0) & (Count) 0
				);
		}

		[TestMethod]
		public void When_Flat_SwitchWithoutOverlap()
		{
			OnFlatList(5)
				.Switch(CreateFlat((42, 5)))
				.ShouldBe
				(
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 4,
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 3,
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 2,
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 1,
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 0,
					e => e & (CollectionChange.ItemInserted, 0) & (Count) 1 & (0, new MyItem(42)),
					e => e & (CollectionChange.ItemInserted, 1) & (Count) 2 & (1, new MyItem(43)),
					e => e & (CollectionChange.ItemInserted, 2) & (Count) 3 & (2, new MyItem(44)),
					e => e & (CollectionChange.ItemInserted, 3) & (Count) 4 & (3, new MyItem(45)),
					e => e & (CollectionChange.ItemInserted, 4) & (Count) 5 & (4, new MyItem(46))
				);
		}

		[TestMethod]
		public void When_Flat_SwitchWithOverlap()
		{
			OnFlatList(5)
				.Switch(CreateFlat((2, 5)))
				.ShouldBe
				(
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 4,
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 3,
					e => e & (CollectionChange.ItemInserted, 3) & (Count) 4 & (3, new MyItem(5)),
					e => e & (CollectionChange.ItemInserted, 4) & (Count) 5 & (4, new MyItem(6))
				);
		}

		[TestMethod]
		public void When_Flat_SwitchWithUpdated()
		{
			IObservableCollection<MyItem> withUpdated;
			//using (_schedulers.Background().AsCurrent())
			{
				withUpdated = CreateFlat(5);
				withUpdated[2] = withUpdated[2].Update();
			}

			OnFlatList(5)
				.Switch(withUpdated)
				.ShouldBe
				(
					e => e & (CollectionChange.ItemChanged, 2) & (Count) 5 & (2, new MyItem(2, 1))
				);
		}

		[TestMethod]
		public void When_Flat_SwitchWithToEquals()
		{
			OnFlatList(5)
				.Switch(CreateFlat(5))
				.ShouldBe( /* nothing */);
		}

		[TestMethod]
		public void When_Flat_SwitchWithToEmpty()
		{
			OnFlatList(5)
				.Switch(CreateFlat((5, 0)))
				.ShouldBe
				(
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 4,
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 3,
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 2,
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 1,
					e => e & (CollectionChange.ItemRemoved, 0) & (Count) 0
				);
		}

		[TestMethod]
		public void When_Grouped_InsertFirstGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s.Insert(0, CreateGroup(42, 2))
				)
				.ShouldBe
				(
					g => g.Groups & (CollectionChange.ItemInserted, 0) & (0, CreateViewGroup(42, 2)),
					// Insert on root are raised by decreasing indexes
					g => g.Root & (CollectionChange.ItemInserted, 1) & (Count) 17, // Reflect the final count a FlatView
					g => g.Root & (CollectionChange.ItemInserted, 0) & (Count) 17
				);
		}

		[TestMethod]
		public void When_Grouped_InsertGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s.Insert(1, CreateGroup(42, 2))
				)
				.ShouldBe
				(
					g => g.Groups & (CollectionChange.ItemInserted, 1) & (1, CreateViewGroup(42, 2)),
					// Insert on root are raised by decreasing indexes
					g => g.Root & (CollectionChange.ItemInserted, 6) & (Count) 17, // Reflect the final count a FlatView
					g => g.Root & (CollectionChange.ItemInserted, 5) & (Count) 17
				);
		}

		[TestMethod]
		public void When_Grouped_InsertLastGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s.Add(CreateGroup(42, 2))
				)
				.ShouldBe
				(
					g => g.Groups & (CollectionChange.ItemInserted, 3) & (3, CreateViewGroup(42, 2)),
					// Insert on root are raised by decreasing indexes
					g => g.Root & (CollectionChange.ItemInserted, 16) & (Count) 17, // Reflect the final count a FlatView
					g => g.Root & (CollectionChange.ItemInserted, 15) & (Count) 17
				);
		}

		[TestMethod]
		public void When_Grouped_InsertFirstEmptyGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s.Insert(0, CreateGroup(42, 0))
				)
				.ShouldBe
				(
					g => g.Groups & (CollectionChange.ItemInserted, 0) & (0, CreateViewGroup(42, 0))
				);
		}

		[TestMethod]
		public void When_Grouped_InsertEmptyGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s.Insert(1, CreateGroup(42, 0))
				)
				.ShouldBe
				(
					g => g.Groups & (CollectionChange.ItemInserted, 1) & (1, CreateViewGroup(42, 0))
				);
		}

		[TestMethod]
		public void When_Grouped_InsertLastEmptyGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s.Add(CreateGroup(42, 0))
				)
				.ShouldBe
				(
					g => g.Groups & (CollectionChange.ItemInserted, 3) & (3, CreateViewGroup(42, 0))
				);
		}

		[TestMethod]
		public void When_Grouped_InsertFirstItemInFirstGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s[0].Insert(0, new MyItem(42))
				)
				.ShouldBe
				(
					g => g[0] & (CollectionChange.ItemInserted, 0) & (0, new MyItem(42)) & (Count) 6,
					g => g.Root & (CollectionChange.ItemInserted, 0) & (0, new MyItem(42)) & (Count) 16
				);
		}

		[TestMethod]
		public void When_Grouped_InsertItemInFirstGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s[0].Insert(2, new MyItem(42))
				)
				.ShouldBe
				(
					g => g[1] & (CollectionChange.ItemInserted, 2) & (2, new MyItem(42)) & (Count) 6,
					g => g.Root & (CollectionChange.ItemInserted, 2) & (2, new MyItem(42)) & (Count) 16
				);
		}

		[TestMethod]
		public void When_Grouped_InsertLastItemInFirstGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s[0].Add(new MyItem(42))
				)
				.ShouldBe
				(
					g => g[0] & (CollectionChange.ItemInserted, 5) & (5, new MyItem(42)) & (Count) 6,
					g => g.Root & (CollectionChange.ItemInserted, 5) & (5, new MyItem(42)) & (Count) 16
				);
		}

		[TestMethod]
		public void When_Grouped_InsertFirstItemInGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s[1].Insert(0, new MyItem(42))
				)
				.ShouldBe
				(
					g => g[1] & (CollectionChange.ItemInserted, 0) & (0, new MyItem(42)) & (Count) 6,
					g => g.Root & (CollectionChange.ItemInserted, 5) & (5, new MyItem(42)) & (Count) 16
				);
		}

		[TestMethod]
		public void When_Grouped_InsertItemInGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s[1].Insert(2, new MyItem(42))
				)
				.ShouldBe
				(
					g => g[1] & (CollectionChange.ItemInserted, 2) & (2, new MyItem(42)) & (Count) 6,
					g => g.Root & (CollectionChange.ItemInserted, 7) & (7, new MyItem(42)) & (Count) 16
				);
		}

		[TestMethod]
		public void When_Grouped_InsertLastItemInGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s[1].Add(new MyItem(42))
				)
				.ShouldBe
				(
					g => g[1] & (CollectionChange.ItemInserted, 5) & (5, new MyItem(42)) & (Count) 6,
					g => g.Root & (CollectionChange.ItemInserted, 10) & (10, new MyItem(42)) & (Count) 16
				);
		}

		[TestMethod]
		public void When_Grouped_InsertFirstItemInLastGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s[2].Insert(0, new MyItem(42))
				)
				.ShouldBe
				(
					g => g[2] & (CollectionChange.ItemInserted, 0) & (0, new MyItem(42)) & (Count) 6,
					g => g.Root & (CollectionChange.ItemInserted, 10) & (10, new MyItem(42)) & (Count) 16
				);
		}

		[TestMethod]
		public void When_Grouped_InsertItemInLastGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s[2].Insert(2, new MyItem(42))
				)
				.ShouldBe
				(
					g => g[2] & (CollectionChange.ItemInserted, 2) & (2, new MyItem(42)) & (Count) 6,
					g => g.Root & (CollectionChange.ItemInserted, 12) & (12, new MyItem(42)) & (Count) 16
				);
		}

		[TestMethod]
		public void When_Grouped_InsertLastItemInLastGroup()
		{
			OnGroupedList(5, 5, 5)
				.Do
				(
					s => s[2].Add(new MyItem(42))
				)
				.ShouldBe
				(
					g => g[2] & (CollectionChange.ItemInserted, 5) & (5, new MyItem(42)) & (Count) 6,
					g => g.Root & (CollectionChange.ItemInserted, 15) & (15, new MyItem(42)) & (Count) 16
				);
		}

		[TestMethod]
		public void When_Grouped_SwitchWithItemUpdated()
		{
			IObservableCollection<MyGroup> withUpdated;
			//using (_schedulers.Background().AsCurrent())
			{
				withUpdated = CreateGrouped(5, 5, 5);
				withUpdated[1][2] = withUpdated[1][2].Update();
			}

			OnGroupedList(5, 5, 5)
				.Switch(withUpdated)
				.ShouldBe
				(
					g => g[1] & (CollectionChange.ItemChanged, 2) & (Count) 5 & (2, new MyItem(2, 1)),
					g => g.Root & (CollectionChange.ItemChanged, 7) & (Count) 15 & (7, new MyItem(2, 1))
				);
		}

		[TestMethod]
		public void When_Grouped_SwitchWithReset()
		{
			// Check note "KNOWN LIMITATION ABOUT 'RESET' IN GROUPED COLLECTION" in 'BranchStrategy'

			OnGroupedList(5, 5, 5)
				.Switch(CreateGrouped(5, 5, 5), TrackingMode.Reset)
				.ShouldBe
				(
					g => g.Groups & (CollectionChange.Reset, 0) & (Count) 0,
					g => g.Groups & (CollectionChange.ItemInserted, 0) & (Count)1,
					g => g.Groups & (CollectionChange.ItemInserted, 1) & (Count)2,
					g => g.Groups & (CollectionChange.ItemInserted, 2) & (Count)3,
					g => g.Root & (CollectionChange.Reset, 0) & (Count)15
				);
		}

		[TestMethod]
		public void When_Grouped_SwitchWithTooMuchItems_Then_Reset()
		{
			var v1 = CreateGrouped(
				CreateGroup(0, 0, 1, 3, 5, 7, 9),
				CreateGroup(10, 0, 11, 13, 15, 17, 19),
				CreateGroup(20, 0, 19, 21, 23, 25, 27, 29)
			);
			var v2 = CreateGrouped(
				CreateGroup(0, 0, (1, 1), 2, (3, 1), 4, (5, 1), 6, (7, 1), 8, (9, 1)),
				CreateGroup(10, 0, 10, (11, 1), 12, (13, 1), 14, (15, 1), 16, (17, 1), 18, (19, 1)),
				CreateGroup(20, 0, 20, (21, 1), 22, (23, 1), 24, (25, 1), 26, (27, 1), 28, (29, 1)),
				CreateGroup(30, 0, 30)
			);

			OnGroupedList(v1)
				.Switch(v2, TrackingMode.Auto)
				.ShouldBe
				(
					g => g.Groups & (CollectionChange.Reset, 0) & (Count)0,
					g => g.Groups & (CollectionChange.ItemInserted, 0) & (Count)1,
					g => g.Groups & (CollectionChange.ItemInserted, 1) & (Count)2,
					g => g.Groups & (CollectionChange.ItemInserted, 2) & (Count)3,
					g => g.Groups & (CollectionChange.ItemInserted, 3) & (Count)4,
					g => g.Root & (CollectionChange.Reset, 0) & (Count)30
				);
		}

		[TestMethod]
		public void When_Flat_SwitchWithReset()
		{
			OnFlatList()
				.Switch(CreateFlat(5), TrackingMode.Reset)
				.ShouldBe
				(
					e => e & (CollectionChange.Reset, 0) & (Count)5
				);
		}

		[TestMethod]
		public void When_Flat_SwitchWithTooMuchItems_Then_Reset()
		{
			var v1 = CreateFlat(1, 3, 5, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29);
			var v2 = CreateFlat((1, 1), 2, (3, 1), 4, (5, 1), 6, (7, 1), 8, (9, 1), 10, (11, 1), 12, (13, 1), 14, (15, 1), 16, (17, 1), 18, (19, 1), 20, (21, 1), 22, (23, 1), 24, (25, 1), 26, (27, 1), 28, (29, 1), 30);

			OnFlatList(v1)
				.Switch(v2, TrackingMode.Auto)
				.ShouldBe
				(
					e => e & (CollectionChange.Reset, 0) & (Count)30
				);
		}

		[TestMethod]
		public void When_Grouped_EnumerateGroupsWhileRemovingOne()
		{
			var v1 = CreateGrouped(
				(0, 0, 0, 5),
				(1, 0, 0, 5),
				(2, 0, 0, 5)
			);
			var v2 = CreateGrouped(
				(0, 0, 0, 5),
				(2, 0, 0, 5)
			);

			OnGroupedList(v1)
				.Switch(v2)
				.ShouldBe
				(
					g => g.Root & (CollectionChange.ItemRemoved, 9),
					g => g.Root & (CollectionChange.ItemRemoved, 8),
					g => g.Root & (CollectionChange.ItemRemoved, 7),
					g => g.Root & (CollectionChange.ItemRemoved, 6),
					g => g.Root & (CollectionChange.ItemRemoved, 5),
					g => g.Groups & (CollectionChange.ItemRemoved, 1) & (Count) 2
				);
		}

		[TestMethod]
		public void When_Grouped_EnumerateGroupsWhileRemovingSome()
		{
			var v1 = CreateGrouped(
				(0, 0, 0, 5),
				(1, 0, 0, 5),
				(2, 0, 0, 5),
				(3, 0, 0, 5), 
				(4, 0, 0, 5)
			);
			var v2 = CreateGrouped(
				(0, 0, 0, 5),
				(2, 0, 0, 5)
			);

			OnGroupedList(v1)
				.Switch(v2)
				.ShouldBe
				(
					g => g.Root & (CollectionChange.ItemRemoved, 9),
					g => g.Root & (CollectionChange.ItemRemoved, 8),
					g => g.Root & (CollectionChange.ItemRemoved, 7),
					g => g.Root & (CollectionChange.ItemRemoved, 6),
					g => g.Root & (CollectionChange.ItemRemoved, 5),
					g => g.Groups & (CollectionChange.ItemRemoved, 1) & (Count)4,
					g => g.Root & (CollectionChange.ItemRemoved, 14),
					g => g.Root & (CollectionChange.ItemRemoved, 13),
					g => g.Root & (CollectionChange.ItemRemoved, 12),
					g => g.Root & (CollectionChange.ItemRemoved, 11),
					g => g.Root & (CollectionChange.ItemRemoved, 10),
					g => g.Root & (CollectionChange.ItemRemoved, 19), // Invalid index ! Used only on Windows, needs to be fixed.
					g => g.Root & (CollectionChange.ItemRemoved, 18), // Invalid index ! Used only on Windows, needs to be fixed.
					g => g.Root & (CollectionChange.ItemRemoved, 17), // Invalid index ! Used only on Windows, needs to be fixed.
					g => g.Root & (CollectionChange.ItemRemoved, 16), // Invalid index ! Used only on Windows, needs to be fixed.
					g => g.Root & (CollectionChange.ItemRemoved, 15), // Invalid index ! Used only on Windows, needs to be fixed.
					g => g.Groups & (CollectionChange.ItemRemoved, 2) & (Count)3,
					g => g.Groups & (CollectionChange.ItemRemoved, 2) & (Count)2
				);
		}

		private LeafChangesRecorder<MyItem> OnFlatList(int? initial = 5)
			=> OnFlatList(CreateFlat(initial));

		private LeafChangesRecorder<MyItem> OnFlatList(ConcurrentObservableCollection<MyItem> source)
		{
			var sut = BindableCollection.Create(
				source,
				itemComparer: KeyEqualityComparer<MyItem>.Default,
				itemVersionComparer: EqualityComparer<MyItem>.Default,
				schedulersProvider: () => null);

			//_schedulers.Advance();

			var result = new LeafChangesRecorder<MyItem>(source, sut);
			result.RecordVectorChanges();

			return result;
		}

		private BranchChangesRecorder<MyGroup, MyItem> OnGroupedList(params int[] groupsDefinition)
			=> OnGroupedList(CreateGrouped(groupsDefinition));

		private BranchChangesRecorder<MyGroup, MyItem> OnGroupedList(ConcurrentObservableCollection<MyGroup> source)
		{
			var sut = BindableCollection.CreateGrouped(
				source,
				itemComparer: KeyEqualityComparer<MyItem>.Default,
				itemVersionComparer: EqualityComparer<MyItem>.Default,
				keyComparer: KeyEqualityComparer<MyGroup>.Default,
				keyVersionComparer: EqualityComparer<MyGroup>.Default,
				schedulersProvider: () => null);

			//_schedulers.Advance();

			var result = new BranchChangesRecorder<MyGroup, MyItem>(source, sut);
			result.RecordVectorChanges();

			return result;
		}

		private ConcurrentObservableCollection<MyItem> CreateFlat(int? initialCount = 5)
			=> initialCount == null ? CreateFlat(default((int, int))) : CreateFlat((0, initialCount.Value));
		private ConcurrentObservableCollection<MyItem> CreateFlat((int index, int count)? initial)
		{
			var initialItems = initial.HasValue
				? Enumerable.Range(initial.Value.index, initial.Value.count).Select(i => new MyItem(i)).ToImmutableList()
				: null;
			var source = new ConcurrentObservableCollection<MyItem>(initialItems ?? Enumerable.Empty<MyItem>());

			return source;
		}
		private ConcurrentObservableCollection<MyItem> CreateFlat(params MyItem[] items) => new(items);


		private ConcurrentObservableCollection<MyGroup> CreateGrouped(params int[] groupsDefinition)
			=> CreateGrouped(groupsDefinition.Select((groupCount, groupIndex) => (groupIndex, 0, 0, groupCount)).ToArray());
		private ConcurrentObservableCollection<MyGroup> CreateGrouped(params (int keyId, int keyVersion, int itemsIndex, int itemsCount)[] groupsDefinition)
		{
			var initialGroups = groupsDefinition?.Length > 0
				? groupsDefinition.Select(group => CreateGroup(group.keyId, group.keyVersion, group.itemsIndex, group.itemsCount))
				: null;
			var source = new ConcurrentObservableCollection<MyGroup>(initialGroups ?? Enumerable.Empty<MyGroup>());

			return source;
		}
		private ConcurrentObservableCollection<MyGroup> CreateGrouped(params MyGroup[] groups)
			=> new ConcurrentObservableCollection<MyGroup>(groups);
		private ConcurrentObservableCollection<MyGroup> CreateGrouped(params MyItem[][] items)
			=> new(items.Select((groupItems, groupId) => CreateGroup(groupId, 0, groupItems)).ToArray());

		private MyGroup CreateGroup(int id, int items = 5)
			=> CreateGroup(id, 0, 0, items);
		private MyGroup CreateGroup(int keyId, int keyVersion, int itemsStart, int itemsCount)
			=> new MyGroup(
				new MyKey(keyId, keyVersion),
				new ConcurrentObservableCollection<MyItem>(Enumerable.Range(itemsStart, itemsCount).Select(i => new MyItem(i)).ToImmutableList()));
		private MyGroup CreateGroup(int keyId, int keyVersion, params MyItem[] items)
			=> new MyGroup(
				new MyKey(keyId, keyVersion),
				new ConcurrentObservableCollection<MyItem>(items));

		private MyCollectionViewGroup CreateViewGroup(int id, int items = 5)
			=> new MyCollectionViewGroup(
				new MyGroup(
					new MyKey(id),
					new ConcurrentObservableCollection<MyItem>(Enumerable.Range(0, items).Select(i => new MyItem(i)).ToImmutableList())
					)
				);
	}

	internal class ConcurrentObservableCollection<T> : IObservableCollection<T>, IObservableCollectionSnapshot, IObservableCollectionSnapshot<T>
	{
		private readonly List<T> _inner;

		public ConcurrentObservableCollection(IEnumerable<T> items)
		{
			_inner = items.ToList();
		}

		/// <inheritdoc />
		public IEnumerator<T> GetEnumerator()
			=> _inner.GetEnumerator();

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
			=> _inner.GetEnumerator();

		/// <inheritdoc />
		public void CopyTo(Array array, int index)
			=> ((ICollection)_inner).CopyTo(array, index);

		/// <inheritdoc />
		public int IndexOf(T item, int startIndex, IEqualityComparer<T>? comparer = null)
			=> _inner.IndexOf(item, startIndex, comparer);

		/// <inheritdoc />
		public int Count => _inner.Count;

		/// <inheritdoc />
		public bool IsReadOnly => ((IList)_inner).IsReadOnly;

		/// <inheritdoc />
		public bool Remove(T item)
		{
			var index = _inner.IndexOf(item);
			if (index < 0)
			{
				return false;
			}

			_inner.RemoveAt(index);
			CollectionChanged?.Invoke(this, RichNotifyCollectionChangedEventArgs.Remove(item, index));

			return true;
		}

		/// <inheritdoc />
		public void AddRange(IReadOnlyList<T> items)
		{
			var index = Count;
			_inner.AddRange(items);
			CollectionChanged?.Invoke(this, RichNotifyCollectionChangedEventArgs.AddSome(items.ToList(), index));
		}

		/// <inheritdoc />
		public void ReplaceRange(int index, int count, IReadOnlyList<T> newItems)
		{
			var oldItems = new T[count];
			_inner.CopyTo(index, oldItems, 0, count);
			_inner.RemoveRange(index, count);
			_inner.InsertRange(index, newItems);

			CollectionChanged?.Invoke(this, RichNotifyCollectionChangedEventArgs.ReplaceSome(oldItems, newItems.ToList(), index));
		}

		/// <inheritdoc />
		int ICollection<T>.Count => _inner.Count;

		/// <inheritdoc />
		bool ICollection<T>.IsReadOnly => ((ICollection<T>)_inner).IsReadOnly;

		/// <inheritdoc />
		int ICollection.Count => _inner.Count;

		/// <inheritdoc />
		public bool IsSynchronized => ((ICollection)_inner).IsSynchronized;

		/// <inheritdoc />
		public object SyncRoot => ((ICollection)_inner).SyncRoot;

		/// <inheritdoc />
		public int Add(object? value)
			=> AddCore((T)(value ?? throw new ArgumentNullException("value is null")));


		/// <inheritdoc />
		public void RemoveAt(int index)
		{
			var item = _inner[index];
			_inner.RemoveAt(index);
			CollectionChanged?.Invoke(this, RichNotifyCollectionChangedEventArgs.Remove(item, index));
		}

		/// <inheritdoc />
		public void Add(T item)
			=> AddCore(item);

		private int AddCore(T item)
		{
			var index = Count;
			_inner.Add(item);
			CollectionChanged?.Invoke(this, RichNotifyCollectionChangedEventArgs.Add(item, index));

			return index;
		}

		/// <inheritdoc />
		public void Clear()
		{
			var oldItems = _inner.ToList();
			_inner.Clear();
			CollectionChanged?.Invoke(this, RichNotifyCollectionChangedEventArgs.Reset(oldItems, Array.Empty<T>()));
		}

		/// <inheritdoc />
		public bool Contains(T item)
			=> _inner.Contains(item);

		/// <inheritdoc />
		public void CopyTo(T[] array, int arrayIndex)
			=> _inner.CopyTo(array, arrayIndex);

		/// <inheritdoc />
		public bool Contains(object? value)
			=> _inner.Contains((T)(value ?? throw new ArgumentNullException("value is null")));

		/// <inheritdoc />
		public int IndexOf(object? value)
			=> _inner.IndexOf((T)(value ?? throw new ArgumentNullException("value is null")));

		/// <inheritdoc />
		public void Insert(int index, object? value)
			=> Insert(index, (T)(value ?? throw new ArgumentNullException("value is null")));

		/// <inheritdoc />
		bool IObservableCollection.Remove(object value)
			=> Remove((T)(value ?? throw new ArgumentNullException("value is null")));

		/// <inheritdoc />
		public IDisposable AddCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current)
		{
			void OnCollectionChanged(object? snd, NotifyCollectionChangedEventArgs e)
				=> callback((RichNotifyCollectionChangedEventArgs)e);

			CollectionChanged += OnCollectionChanged;
			current = this;

			return Disposable.Create(() => CollectionChanged -= OnCollectionChanged);
		}

		/// <inheritdoc />
		public void RemoveCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current)
			=> throw new NotImplementedException();

		/// <inheritdoc />
		public IDisposable AddCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<T> current)
		{
			void OnCollectionChanged(object? snd, NotifyCollectionChangedEventArgs e)
				=> callback((RichNotifyCollectionChangedEventArgs)e);

			CollectionChanged += OnCollectionChanged;
			current = this;

			return Disposable.Create(() => CollectionChanged -= OnCollectionChanged);
		}

		/// <inheritdoc />
		public void RemoveCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot<T> current)
			=> throw new NotImplementedException();

		/// <inheritdoc />
		public void Remove(object? value)
			=> Remove((T)(value ?? throw new ArgumentNullException("value is null")));

		/// <inheritdoc />
		public int IndexOf(T item)
			=> _inner.IndexOf(item);

		/// <inheritdoc />
		public void Insert(int index, T item)
		{
			_inner.Insert(index, item);
			CollectionChanged?.Invoke(this, RichNotifyCollectionChangedEventArgs.Add(item, index));
		}

		/// <inheritdoc />
		void IList<T>.RemoveAt(int index)
			=> RemoveAt(index);

		/// <inheritdoc />
		public T this[int index]
		{
			get => _inner[index];
			set
			{
				var oldItem = _inner[index];
				_inner[index] = value;
				CollectionChanged?.Invoke(this, RichNotifyCollectionChangedEventArgs.Replace(oldItem, value, index));
			}
		}

		/// <inheritdoc />
		public int IndexOf(object item, int startIndex, IEqualityComparer? comparer = null)
			=> _inner.IndexOf((T)item, startIndex, Count, comparer);

		/// <inheritdoc />
		void IList.RemoveAt(int index)
			=> RemoveAt(index);

		/// <inheritdoc />
		public bool IsFixedSize => ((IList)_inner).IsFixedSize;

		/// <inheritdoc />
		bool IList.IsReadOnly => ((IList)_inner).IsReadOnly;

		/// <inheritdoc />
		object? IList.this[int index]
		{
			get => this[index];
			set => this[index] = (T)(value ?? throw new ArgumentNullException("value is null"));
		}

		/// <inheritdoc />
		public event NotifyCollectionChangedEventHandler? CollectionChanged;
	}
}
