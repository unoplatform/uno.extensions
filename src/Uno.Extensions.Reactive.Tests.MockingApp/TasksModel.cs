using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Tests.MockingApp;

public interface ITaskService
{
	Task<IImmutableList<TaskItem>> GetTasks(CancellationToken ct);
}

public partial record TaskItem(string Id, string Title);

/// <summary>
/// Fixture for a service-dependent input declared as a list state rather than a list feed, the shape a model
/// uses for a list it edits, and for a feed derived from it.
/// </summary>
public partial record TasksModel(ITaskService Service)
{
	public IListState<TaskItem> Tasks => ListState.Async(this, async ct => await Service.GetTasks(ct));

	public IFeed<int> TasksCount => Tasks.AsFeed().Select(tasks => tasks.Count);
}
