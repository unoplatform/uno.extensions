using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Tests.MockingApp;

public interface ISearchService
{
	ValueTask<IImmutableList<string>> Search(string query, CancellationToken ct);
}

/// <summary>
/// Fixture for a feed that reads both a service and another feed of the model: a search over a query state. Left to
/// its real logic it would call the null-injected service, so the mock requires it like any other input.
/// </summary>
public partial record SearchModel(ISearchService Service)
{
	// independent state — not part of the mock
	public IState<string> Query => State<string>.Value(this, () => "");

	// reads the service and the query — an input
	public IListFeed<string> Results => Query
		.SelectAsync(async (query, ct) => await Service.Search(query ?? "", ct))
		.AsListFeed();
}
