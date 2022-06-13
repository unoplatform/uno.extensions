using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Linq;
using Windows.Foundation.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Umbrella.Presentation.Feeds.Tests.Collections._TestUtils;
using Uno.Extensions.Collections;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;
using static Uno.Extensions.Collections.CollectionChanged;

namespace Umbrella.Presentation.Feeds.Tests.Collections._BindableCollection.Facets
{
	[TestClass]
    public class Given_FlatCollectionChangedFacet
    {
		[TestMethod]
		public void When_RaiseInnerVectorEvent_ThenEventPropagated()
		{
			var source = new TestCollectionView
			{
				CollectionGroups = new TestObservableVector
				{
				}
			};
			var sut = new FlatCollectionChangedFacet(() => source);
			var result = new List<(object? sender, IVectorChangedEventArgs args)>();
			sut.AddVectorChangedHandler((snd, args) => result.Add((snd, args)));

			var group = new MyCollectionViewGroup(null, new TestObservableVector());
			source.CollectionGroups.Add(group);
			var groupEvents = new CollectionChangedFacet(() => group.GroupItems);
			sut.AddChild(groupEvents, null!, null);

			groupEvents.VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.ItemChanged, 0));

			ShouldBe(
				result,
				(source, CollectionChange.ItemChanged, 0));
		}

		[TestMethod]
		public void When_RaiseInnerVectorEvent_ThenEventPropagatedWithCorrectedIndex()
		{
			var source = new TestCollectionView
			{
				CollectionGroups = new TestObservableVector
				{
					new MyCollectionViewGroup(null, new TestObservableVector { new object(), new object(), new object() })
				}
			};
			var sut = new FlatCollectionChangedFacet(() => source);
			var result = new List<(object? sender, IVectorChangedEventArgs args)>();
			sut.AddVectorChangedHandler((snd, args) => result.Add((snd, args)));

			var group = new MyCollectionViewGroup(null, new TestObservableVector());
			source.CollectionGroups.Add(group);
			var groupEvents = new CollectionChangedFacet(() => group.GroupItems);
			sut.AddChild(groupEvents, null!, null);

			groupEvents.VectorChanged?.Invoke(new VectorChangedEventArgs(CollectionChange.ItemChanged, 0));

			ShouldBe(
				result,
				(source, CollectionChange.ItemChanged, 3));
		}

		[TestMethod]
		public void When_AddInnerWithSnapshot_ThenRaiseVectorAddEvents()
		{
			var source = new TestCollectionView
			{
				CollectionGroups = new TestObservableVector
				{
					new MyCollectionViewGroup(null, new TestObservableVector { new object(), new object(), new object() })
				}
			};
			var sut = new FlatCollectionChangedFacet(() => source);

			var result = new List<(object? sender, IVectorChangedEventArgs args)>();
			sut.AddVectorChangedHandler((snd, args) => result.Add((snd, args)));

			var group = new MyCollectionViewGroup(null, new TestObservableVector { new object(), new object(), new object() });
			source.CollectionGroups.Add(group);
			sut.AddChild(new CollectionChangedFacet(() => null!), group.GroupItems, new ObservableCollectionSnapshot<object?>(group.GroupItems.ToImmutableList()));

			ShouldBe(
				result,
				(source, CollectionChange.ItemInserted, 5),
				(source, CollectionChange.ItemInserted, 4),
				(source, CollectionChange.ItemInserted, 3));
		}

		[TestMethod]
		public void When_RaiseInnerCollectionEvent_ThenEventPropagated()
		{
			var groups = new TestObservableVector();
			var sender = new TestCollectionView { CollectionGroups = groups };

			var sut = new FlatCollectionChangedFacet(() => sender);
			var result = new List<(object? sender, NotifyCollectionChangedEventArgs args)>();
			sut.AddCollectionChangedHandler((snd, args) => result.Add((snd, args)));

			var group = new MyCollectionViewGroup(null, new TestObservableVector());
			groups.Add(group);
			var groupEvents = new CollectionChangedFacet(() => group.GroupItems);
			sut.AddChild(groupEvents, null!, null);

			var item = new object();
			groupEvents.CollectionChanged?.Invoke(Add(item, 0));

			ShouldBe(
				result,
				(sender, Add(item, 0)));
		}

		[TestMethod]
		public void When_RaiseInnerCollectionEvent_ThenEventPropagatedWithCorrectedIndex()
		{
			var groups = new TestObservableVector { new MyCollectionViewGroup(null, new TestObservableVector { new object(), new object(), new object() }) };
			var sender = new TestCollectionView { CollectionGroups = groups };

			var sut = new FlatCollectionChangedFacet(() => sender);
			var result = new List<(object? sender, NotifyCollectionChangedEventArgs args)>();
			sut.AddCollectionChangedHandler((snd, args) => result.Add((snd, args)));

			var group = new MyCollectionViewGroup(null, new TestObservableVector());
			groups.Add(group);
			var groupEvents = new CollectionChangedFacet(() => group.GroupItems);
			sut.AddChild(groupEvents, null!, null);

			var item = new object();
			groupEvents.CollectionChanged?.Invoke(Add(item, 0));

			ShouldBe(
				result,
				(sender, Add(item, 3)));
		}

		[TestMethod]
		public void When_AddInnerWithSnapshot_ThenRaiseCollectionAddEvents()
		{
			var source = new TestCollectionView
			{
				CollectionGroups = new TestObservableVector
				{
					new MyCollectionViewGroup(null, new TestObservableVector { new object(), new object(), new object() })
				}
			};
			var sut = new FlatCollectionChangedFacet(() => source);

			var result = new List<(object? sender, NotifyCollectionChangedEventArgs args)>();
			sut.AddCollectionChangedHandler((snd, args) => result.Add((snd, args)));

			var group = new MyCollectionViewGroup(null, new TestObservableVector { new object(), new object(), new object() });
			source.CollectionGroups.Add(group);
			sut.AddChild(new CollectionChangedFacet(() => null!), group.GroupItems, new ObservableCollectionSnapshot<object?>(group.GroupItems.ToImmutableList()));

			ShouldBe(
				result,
				(source, AddSome(group.GroupItems.ToList(), 3)));
		}

		[TestMethod]
		public void When_NotSubscribeToEvents_Then_DontSubscribeToInnerEvent()
		{
			//sut.AddChild(groupEvents, group.GroupItems, new ObservableCollectionSnapshot<object>(System.Collections.Immutable.ImmutableList<object>.Empty, null));
		}

		private void ShouldBe(
			List<(object? sender, IVectorChangedEventArgs args)> result, 
			params (object? sender, CollectionChange change, uint index)[] expected)
		{
			Assert.IsTrue(result.SequenceEqual(
				expected.Select(v => (v.sender, new VectorChangedEventArgs(v.change, v.index) as IVectorChangedEventArgs)), 
				ChangeEventArgsEqualityComparer.Instance));
		}

		private void ShouldBe(
			List<(object? sender, NotifyCollectionChangedEventArgs args)> result,
			params (object? sender, NotifyCollectionChangedEventArgs args)[] expected)
		{
			Assert.IsTrue(result.SequenceEqual(expected, ChangeEventArgsEqualityComparer.Instance));
		}

		private record VectorChangedEventArgs(CollectionChange CollectionChange, uint Index) : IVectorChangedEventArgs;
	}
}
