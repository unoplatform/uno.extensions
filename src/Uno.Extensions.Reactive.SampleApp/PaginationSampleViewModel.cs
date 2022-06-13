using System;
using System.Collections.Immutable;
using System.Linq;

namespace Uno.Extensions.Reactive.SampleApp;

public partial class PaginationSampleViewModel
{
	public IListFeed<int> Items => ListFeed<int>.PaginatedBy(async (page, ct) => ImmutableList.CreateRange(Enumerable.Range((int)page.PageNumber * 20, 20)));
}
