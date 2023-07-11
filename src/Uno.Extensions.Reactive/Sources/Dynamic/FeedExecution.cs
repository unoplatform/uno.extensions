using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// Represents an invocation of the main loading operation of a <see cref="DynamicFeed{T}"/> for a given <see cref="FeedSession"/>.
/// </summary>
internal abstract class FeedExecution : IAsyncDisposable
{
	private static readonly AsyncLocal<FeedExecution?> _current = new();
	public static FeedExecution? Current => _current.Value;

	internal static CurrentSubscription SetCurrent(FeedExecution execution)
	{
		var previous = _current.Value;
		_current.Value = execution;
		return new CurrentSubscription(previous);
	}

	private readonly CancellationTokenSource _ct;

	private protected FeedExecution(FeedSession session, IReadOnlyCollection<ExecuteRequest> requests)
	{
		Session = session;
		Requests = requests;

		_ct = CancellationTokenSource.CreateLinkedTokenSource(session.Token);
	}

	/// <summary>
	/// The owning session.
	/// </summary>
	public FeedSession Session { get; }

	/// <summary>
	/// A set of requests that have triggered this execution.
	/// </summary>
	public IReadOnlyCollection<ExecuteRequest> Requests { get; }

	/// <summary>
	/// A cancellation token used to cancel the current execution.
	/// </summary>
	/// <remarks>This token will be cancelled when a new execution is started for the same <see cref="Session"/>.</remarks>
	public CancellationToken Token { get; }

	/// <summary>
	/// Allows a <see cref="IDependency"/> to append custom (meta)data to the message.
	/// </summary>
	/// <param name="updater"></param>
	public abstract void Enqueue(Action<IMessageBuilder> updater);

	internal record struct CurrentSubscription(FeedExecution? Previous) : IDisposable
	{
		/// <inheritdoc />
		public void Dispose()
			=> _current.Value = Previous;
	}

	/// <inheritdoc />
	public virtual async ValueTask DisposeAsync()
	{
		_ct.Cancel();

		if (_current.Value == this)
		{
			// No previous to restore here, it will be restored by the CurrentSubscription.
			// We are only making clear that this execution is no longer the current one.
			_current.Value = null;
		}
	}
}
