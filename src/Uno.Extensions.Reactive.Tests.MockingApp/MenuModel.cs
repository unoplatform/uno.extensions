using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Tests.MockingApp;

public interface IMenuService
{
	Task<IImmutableList<string>> GetItems(CancellationToken ct);
}

public interface IMenuNavigator
{
	ValueTask GoBack(CancellationToken ct);
}

/// <summary>
/// Fixture for a model whose constructor takes more than one dependency: the generated Create must
/// null-inject each parameter. Also carries a derived feed and an independent state for later coverage.
/// </summary>
public partial class MenuModel
{
	private readonly IMenuService _service;
	private readonly IMenuNavigator _navigator;

	public MenuModel(IMenuService service, IMenuNavigator navigator)
	{
		_service = service;
		_navigator = navigator;
	}

	// service-dependent input (list)
	public IListFeed<string> Items => ListFeed.Async(async ct => await _service.GetItems(ct));

	// derived over the service-dependent list
	public IFeed<int> ItemsCount => Items.AsFeed().Select(items => items.Count);

	// independent state — not part of the mock
	public IState<string> Filter => State<string>.Value(this, () => string.Empty);

	// command → IAsyncCommand GoBack on the VM; the navigator is only reached when it executes
	public ValueTask GoBack(CancellationToken ct) => _navigator.GoBack(ct);
}
