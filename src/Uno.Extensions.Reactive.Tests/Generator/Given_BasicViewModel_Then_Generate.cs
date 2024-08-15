using System;
using System.Collections.Immutable;
using System.Linq;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Generator;

[TestClass]
public partial class Given_BasicViewModel_Then_Generate : FeedUITests
{
	// Those are mostly compilation tests!

	[TestMethod]
	public async Task Test_Constructors()
	{
		await using var bindableCtor1 = new Given_BasicViewModel_Then_Generate__ViewViewModel();

		await using var bindableCtor2 = new Given_BasicViewModel_Then_Generate__ViewViewModel(aRandomService: "aRandomService");

		await using var bindableCtor3 = new Given_BasicViewModel_Then_Generate__ViewViewModel(
			anExternalInput: default(IFeed<string>)!,
			anExternalReadWriteInput: default(IState<string>)!,
			anExternalRecordInput: default(IFeed<MyRecord>)!,
			anExternalWeirdRecordInput: default(IFeed<MyWeirdRecord>)!);
	}

	[TestMethod]
	public async Task When_FeedOfKindOfImmutableList_Then_TreatAsListFeed()
	{
		await using var bindable = new When_FeedOfKindOfImmutableList_Then_TreatAsListFeed_ViewViewModel();

		AssertIsValid(bindable.AFeedOfArray);
		AssertIsValid(bindable.AFeedOfImmutableList);
		AssertIsValid(bindable.AFeedOfImmutableQueue);
		AssertIsValid(bindable.AFeedOfImmutableSet);
		AssertIsValid(bindable.AFeedOfImmutableStack);
		AssertIsValid(bindable.AFeedOfImmutableArray);
		AssertIsValid(bindable.AFeedOfImmutableListImpl);
		AssertIsValid(bindable.AFeedOfImmutableQueueImpl);
		AssertIsValid(bindable.AFeedOfImmutableSetImpl);
		AssertIsValid(bindable.AFeedOfImmutableStackImpl);

		static void AssertIsValid(object bindable)
		{
			Assert.IsNotNull(bindable);
			Assert.IsNotNull(bindable as IListState<string>);

			// This is not the case on UI agnostic targets.
			// See When_FeedOfKindOfImmutableList_Then_TreatAsListFeed_And_ICollectionView in runtime tests project
			//Assert.IsNotNull(bindable as ICollectionView);
		}
	}

	public partial class When_FeedOfKindOfImmutableList_Then_TreatAsListFeed_ViewModel
	{
		public IFeed<string[]> AFeedOfArray { get; } = Feed.Async(async ct => Array.Empty<string>());

		public IFeed<IImmutableList<string>> AFeedOfImmutableList { get; } = Feed.Async(async ct => ImmutableList<string>.Empty as IImmutableList<string>);

		public IFeed<IImmutableQueue<string>> AFeedOfImmutableQueue { get; } = Feed.Async(async ct => ImmutableQueue<string>.Empty as IImmutableQueue<string>);

		public IFeed<IImmutableSet<string>> AFeedOfImmutableSet { get; } = Feed.Async(async ct => ImmutableHashSet<string>.Empty as IImmutableSet<string>);

		public IFeed<IImmutableStack<string>> AFeedOfImmutableStack { get; } = Feed.Async(async ct => ImmutableStack<string>.Empty as IImmutableStack<string>);

		public IFeed<ImmutableArray<string>> AFeedOfImmutableArray { get; } = Feed.Async(async ct => ImmutableArray<string>.Empty);

		public IFeed<ImmutableList<string>> AFeedOfImmutableListImpl { get; } = Feed.Async(async ct => ImmutableList<string>.Empty);

		public IFeed<ImmutableQueue<string>> AFeedOfImmutableQueueImpl { get; } = Feed.Async(async ct => ImmutableQueue<string>.Empty);

		public IFeed<ImmutableHashSet<string>> AFeedOfImmutableSetImpl { get; } = Feed.Async(async ct => ImmutableHashSet<string>.Empty);

		public IFeed<ImmutableStack<string>> AFeedOfImmutableStackImpl { get; } = Feed.Async(async ct => ImmutableStack<string>.Empty);
	}

	[TestMethod]
	public async Task When_FeedOfKindOfRawEnumerable_Then_DoNotTreatAsListFeed()
	{
		await using var bindable = new When_FeedOfKindOfRawEnumerable_Then_DoNotTreatAsListFeed_ViewViewModel();

		AssertIsValid(bindable.AFeedOfEnumerable);

		static void AssertIsValid(object bindable)
		{
			Assert.IsNull(bindable as IListState<string>);


			// This is not the case on UI agnostic targets.
			// See When_FeedOfKindOfRawEnumerable_Then_DoNotTreatAsListFeed_And_ICollectionView in runtime tests project
			//Assert.IsNull(bindable as ICollectionView);
		}
	}

	public partial class When_FeedOfKindOfRawEnumerable_Then_DoNotTreatAsListFeed_ViewModel
	{
		public IFeed<IEnumerable<string>> AFeedOfEnumerable { get; } = Feed.Async(async ct => Enumerable.Empty<string>());
	}
}
