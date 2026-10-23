using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Tests.MockingApp;

public interface IPriceService
{
	Task<decimal> GetPrice(CancellationToken ct);
}

/// <summary>
/// Fixture for a scalar input with derivations two levels deep: <see cref="Discounted"/> selects over the input,
/// and <see cref="Rounded"/> selects over that derived feed, so a mock has to travel through both.
/// </summary>
public partial record PriceModel(IPriceService Service)
{
	public IFeed<decimal> Price => Feed.Async(async ct => await Service.GetPrice(ct));

	public IFeed<decimal> Discounted => Price.Select(price => price * 0.9m);

	public IFeed<decimal> Rounded => Discounted.Select(price => decimal.Round(price));
}
