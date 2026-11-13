using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Tests.MockingApp;

public interface IShelfService
{
	ValueTask<IImmutableList<string>> GetBooks(CancellationToken ct);
}

/// <summary>
/// Fixture for a feed written as a method group: binding <c>Service.GetBooks</c> reads the service when the feed is
/// built, and the view-model's constructor builds every feed, so <c>Create</c> cannot pass the service null.
/// </summary>
public partial record ShelfModel(IShelfService Service)
{
	public IListFeed<string> Books => ListFeed.Async(Service.GetBooks);
}
