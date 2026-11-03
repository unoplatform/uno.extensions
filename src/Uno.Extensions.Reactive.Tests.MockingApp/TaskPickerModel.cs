using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Tests.MockingApp;

/// <summary>
/// Fixture for a service-dependent list with a selection, the shape a page uses for a list whose selected item it
/// shows: the selection operator wraps the list in a list state of its own.
/// </summary>
public partial record TaskPickerModel(ITaskService Service)
{
	public IState<TaskItem> Selected => State<TaskItem>.Empty(this);

	public IListFeed<TaskItem> Tasks => ListFeed.Async(async ct => await Service.GetTasks(ct)).Selection(Selected);
}
