using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.SampleApp;

public partial class PaginationSampleViewModel
{
	public IState<double> Delay => State.Value(this, () => 3.0);

	public IState<bool> HasMoreItems => State.Value(this, () => true);

	public IListFeed<int> Items => ListFeed<int>.AsyncPaginated(async (page, ct) =>
	{
		var delay = await Delay;
		if (delay > 0)
		{
			await Task.Delay(TimeSpan.FromSeconds(delay), ct);
		}

		var hasMoreItems = await HasMoreItems;

		return hasMoreItems
			? ImmutableList.CreateRange(Enumerable.Range((int)page.Index * 20, 20))
			: ImmutableList<int>.Empty;
	});
}
