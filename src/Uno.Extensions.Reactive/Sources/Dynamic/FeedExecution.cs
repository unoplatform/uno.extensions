using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// Represents an invocation of the main loading operation of a <see cref="DynamicFeed{T}"/> for a given <see cref="FeedSession"/>.
/// </summary>
internal abstract class FeedExecution : IAsyncDisposable
{
	private static readonly AsyncLocal<FeedExecution?> _current = new();

	/// <summary>
	/// Gets the current execution if any.
	/// This is not null when used within a <see cref="DynamicFeed{T}"/> method, and null otherwise.
	/// </summary>
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
	public CancellationToken Token => _ct.Token;

	/// <summary>
	/// Allows a <see cref="IDependency"/> to append custom (meta)data to the message (cf. Remarks about execution of the update).
	/// </summary>
	/// <param name="updater"></param>
	/// <remarks>
	/// - If the execution is still loading the data (a.k.a. the main action), then updates will be queued and forwarded only when the main action publish a message
	/// (i.e. optionally at the beginning if the message as the be flagged as transient + one at the end to commit either the updated data or the error).
	/// - If the execution completed the load of the data, then the update will be forwarded immediately.
	/// - If the execution has been cancelled (either a new execution has started, either the session has ended), then **the update will be ignored**.
	/// <br />
	/// **WARNING:**
	/// The last case means that during a normal session an update might be dropped if a new execution is started before the data has been loaded/failed and a message has been published.
	/// **Your dependency should be designed to handle this case.**
	/// </remarks>
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
