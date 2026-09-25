using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Tests.MockingApp;

public interface ICartService
{
	Task<decimal> GetSubtotal(CancellationToken ct);

	Task<decimal> GetShipping(CancellationToken ct);
}

/// <summary>
/// Fixture for a derived member that combines two inputs: the MVUX generator emits one <c>[FeedDependency]</c>
/// per input for <see cref="Total"/>, and the mock must still declare it once.
/// </summary>
public partial record CartModel(ICartService Service)
{
	public IFeed<decimal> Subtotal => Feed.Async(async ct => await Service.GetSubtotal(ct));

	public IFeed<decimal> Shipping => Feed.Async(async ct => await Service.GetShipping(ct));

	public IFeed<decimal> Total => Feed.Combine(Subtotal, Shipping).Select(amounts => amounts.Item1 + amounts.Item2);
}
