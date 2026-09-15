using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Tests.MockingApp.SingleProject;

public interface IPantryService
{
	Task<IImmutableList<string>> GetItems(CancellationToken ct);
}

/// <summary>
/// Fixture for the two shapes an app written from the templates actually has: the model is declared as
/// a positional record — the form the reference documentation uses, where the service reaches the feed
/// through the synthesized property rather than through a constructor-body assignment — and it lives in
/// the same compilation as the mocking generator.
/// </summary>
public partial record PantryModel(IPantryService Service)
{
	// service-dependent input, reached through the record's synthesized property
	public IListFeed<string> Items => ListFeed.Async(async ct => await Service.GetItems(ct));

	// derived over the service-dependent list — real logic by default, optional override
	public IFeed<int> ItemsCount => Items.AsFeed().Select(items => items.Count);

	// independent input — not part of the mock
	public IFeed<string> Title => Feed.Async(async ct => "Pantry");
}
