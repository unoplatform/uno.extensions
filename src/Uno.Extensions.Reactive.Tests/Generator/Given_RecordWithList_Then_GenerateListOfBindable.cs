using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Generator;

[TestClass]
public partial class Given_RecordWithList_Then_GenerateListOfBindable : FeedUITests
{
	[TestMethod]
	public async Task IsListOfBindable()
	{
		await using var sut = new MyViewViewModel();

		sut.MyFeed.Items.Should().BeAssignableTo<IEnumerable<MyRecordWithListItemViewModel>>();
	}

	[TestMethod]
	public async Task SupportsAdd()
	{
		await using var sut = new MyViewViewModel();
		var args = new List<NotifyCollectionChangedEventArgs>();
		var result = sut.Model.MyFeed.Select(r => r.Items).Record();

		await WaitForInitialValue(sut, s => s.MyFeed);
		await ExecuteOnDispatcher(() =>
		{
			sut.MyFeed.Items.CollectionChanged += (snd, e) => args.Add(e);
			sut.MyFeed.Items.Add(new MyRecordWithListItem(42));
		});

		await result
			.Should()
			.BeAsync(m => m
				.Message(Items.Some<MyRecordWithListItem>(new(0), new(1), new(2)))
				.Message(Items.Some<MyRecordWithListItem>(new (0), new(1), new(2), new(42)))
			);
	}

	[TestMethod]
	public async Task SupportsRemove()
	{
		await using var sut = new MyViewViewModel();
		var args = new List<NotifyCollectionChangedEventArgs>();
		var result = sut.Model.MyFeed.Select(r => r.Items).Record();

		await WaitForInitialValue(sut, s => s.MyFeed);
		await ExecuteOnDispatcher(() =>
		{
			sut.MyFeed.Items.CollectionChanged += (snd, e) => args.Add(e);
			sut.MyFeed.Items.RemoveAt(1);
		});

		await result
			.Should()
			.BeAsync(m => m
				.Message(Items.Some<MyRecordWithListItem>(new(0), new(1), new(2)))
				.Message(Items.Some<MyRecordWithListItem>(new(0), new(2)))
			);
	}

	public partial class MyViewModel
	{
		public IState<MyRecordWithList> MyFeed => State.Async(this, async ct => new MyRecordWithList(Enumerable.Range(0, 3).Select(i => new MyRecordWithListItem(i)).ToImmutableList()));
	}

	public partial record MyRecordWithList(ImmutableList<MyRecordWithListItem> Items);

	public partial record MyRecordWithListItem(int Id);
}
