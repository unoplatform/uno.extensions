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

public interface IMenuSnapshot
{
	IImmutableList<string> Items { get; }
}

/// <summary>
/// Fixture for the constructor shapes the generated Create must handle: more than one dependency (each
/// parameter null-injected) and two public constructors of equal arity (the parameter type must be spelled
/// out, otherwise <c>new MenuViewModel(default!, default!)</c> is ambiguous, CS0121). The derived feed and
/// the independent state pin the record's member classification: the derived feed is an optional override,
/// the independent state is left out.
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

	public MenuModel(IMenuSnapshot snapshot, IMenuNavigator navigator)
		: this(new SnapshotMenuService(snapshot), navigator)
	{
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

internal sealed class SnapshotMenuService : IMenuService
{
	private readonly IMenuSnapshot _snapshot;

	public SnapshotMenuService(IMenuSnapshot snapshot)
	{
		_snapshot = snapshot;
	}

	public Task<IImmutableList<string>> GetItems(CancellationToken ct) => Task.FromResult(_snapshot.Items);
}
