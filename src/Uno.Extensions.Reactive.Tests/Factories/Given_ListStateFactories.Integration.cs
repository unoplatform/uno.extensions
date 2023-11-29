using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;
using static System.Linq.Enumerable;

namespace Uno.Extensions.Reactive.Tests.Factories;

public partial class Given_ListStateFactories
{
	[TestMethod]
	public async Task When_UpdateToEmptyList_Then_GoesToNone()
	{
		var (result, sut) = ListState<int>.Async(this, async ct => Range(0, 20).ToImmutableList() as IImmutableList<int>).Record();

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data, Items.Reset(Range(0, 20)))
				.Current(Items.Range(20), Error.No, Progress.Final))
		);

		await sut.UpdateAsync(_ => ImmutableList<int>.Empty, CT);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data, Items.Reset(Range(0, 20)))
				.Current(Items.Range(20), Error.No, Progress.Final))
			.Message(m => m
				.Changed(Changed.Data, Items.Reset(Range(0, 20), Empty<int>()))
				.Current(Data.None, Error.No, Progress.Final))
		);
	}

	[TestMethod]
	public async Task When_AsyncPaginated_Then_CurrentCountReflectsUpdates()
	{
		var sut = ListState<int>.PaginatedAsync(this, async (req, ct) => Range((int)req.CurrentCount, (int)(req.DesiredSize ?? 2)).ToImmutableList());
		var requests = new RequestSource();
		var ctx = Context.SourceContext.CreateChild(requests);

		var result = sut.Record(ctx);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 2)))
				.Current(Items.Range(2), Error.No, Progress.Final, Pagination.HasMore))
		);

		await sut.InsertAsync(42, CT);
		requests.RequestMoreItems(3);

		result.Should().Be(b => b
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Reset(Range(0, 2)))
				.Current(Items.Range(2), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data, Items.Add(at: 0, items: 42))
				.Current(Items.Some(42, 0, 1), Error.No, Progress.Final, Pagination.HasMore))
			.Message(m => m
				.Changed(Changed.Data & Changed.Pagination, Items.Add(at: 3, items: Range(3, 3)))
				.Current(Items.Some(42, 0, 1, 3, 4, 5), Error.No, Progress.Final, Pagination.HasMore))
		);
	}
}
