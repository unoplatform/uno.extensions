using System;
using System.Collections.Immutable;
using System.Linq;
using System.Windows.Input;
using Windows.ApplicationModel.AppService;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace Uno.Extensions.Reactive.UI.Tests.Generator;

[TestClass]
public partial class Given_BasicViewModel_Then_Generate
{
	[TestMethod]
	public async Task When_FeedOfKindOfImmutableList_Then_TreatAsListFeed_And_ICollectionView()
	{
		await using var bindable = new BindableWhen_FeedOfKindOfImmutableList_Then_TreatAsListFeed_ViewModel();

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
			Assert.IsNotNull(bindable as ICollectionView);
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
	public async Task When_FeedOfKindOfRawEnumerable_Then_DoNotTreatAsListFeed_And_ICollectionView()
	{
		await using var bindable = new BindableWhen_FeedOfKindOfRawEnumerable_Then_DoNotTreatAsListFeed_ViewModel();

		AssertIsValid(bindable.AFeedOfEnumerable);

		static void AssertIsValid(object bindable)
		{
			Assert.IsNull(bindable as IListState<string>);
			Assert.IsNull(bindable as ICollectionView);
		}
	}

	public partial class When_FeedOfKindOfRawEnumerable_Then_DoNotTreatAsListFeed_ViewModel
	{
		public IFeed<IEnumerable<string>> AFeedOfEnumerable { get; } = Feed.Async(async ct => Enumerable.Empty<string>());
	}
}
