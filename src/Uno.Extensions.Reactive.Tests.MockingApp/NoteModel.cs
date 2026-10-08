using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Messaging;

namespace Uno.Extensions.Reactive.Tests.MockingApp;

public partial record Note(int Id, string Text);

public interface INoteService
{
	Task<Note> GetPinned(CancellationToken ct);
}

/// <summary>
/// Fixture for the MVUX messaging pattern: the constructor observes a state on the messenger it is given, so
/// <c>Create</c> cannot pass it null.
/// </summary>
public partial class NoteModel
{
	private readonly INoteService _service;

	public NoteModel(INoteService service, IMessenger messenger)
	{
		_service = service;
		messenger.Observe(Pinned, note => note.Id);
	}

	// service-dependent input (state), kept in sync with the messenger
	public IState<Note> Pinned => State.Async(this, async ct => await _service.GetPinned(ct));

	// derived over the observed state
	public IFeed<string> PinnedText => Pinned.Select(note => note.Text);
}
