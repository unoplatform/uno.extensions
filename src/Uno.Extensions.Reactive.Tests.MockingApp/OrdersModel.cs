using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Tests.MockingApp;

public interface IOrderService
{
	Task<IImmutableList<string>> GetOrders(CancellationToken ct);
}

/// <summary>
/// Fixture that counts the runs of its real loader, for a list a page shows and a list derived from it.
/// </summary>
public partial record OrdersModel(IOrderService Service)
{
	private static int _loads;

	public static int Loads => Volatile.Read(ref _loads);

	public IListFeed<string> Orders => ListFeed.Async(async ct =>
	{
		Interlocked.Increment(ref _loads);
		return await Service.GetOrders(ct);
	});

	public IState<string> Filter => State.Value(this, () => string.Empty);

	public IListFeed<string> VisibleOrders => Feed.Combine(Orders.AsFeed(), Filter)
		.Select(x => (IImmutableList<string>)x.Item1.Where(order => order.Contains(x.Item2)).ToImmutableList())
		.AsListFeed();
}
