using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Testing;

/// <summary>
/// An helper class that subscribes to a feed and store all messages produces by that feed so we can assert the content of those messages.
/// </summary>
/// <typeparam name="TFeed">Type of the feed under test.</typeparam>
/// <typeparam name="TValue">Type of the value of the feed.</typeparam>
public class FeedRecorder<TFeed, TValue> : IFeedRecorder<TValue>, ICollection<Message<TValue>>, IDisposable
	where TFeed : ISignal<Message<TValue>>
{
	private event EventHandler? _messageAdded;

	private readonly Func<FeedRecorder<TFeed, TValue>, TFeed> _feedProvider;
	private readonly SourceContext _context;
	private readonly CancellationTokenSource _ct = new();
	private readonly List<Message<TValue>> _messages = new();

	private int _enumerationStarted;
	private Task? _enumeration;
	private TFeed? _feed;

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	/// <param name="feedProvider">A delegate to get the source, which will be invoked only when this recorder is being <see cref="Enable"/>.</param>
	/// <param name="context">The context to use to subscribe to the source.</param>
	/// <param name="autoEnable">Determines if the recorder to be automatically <see cref="Enable"/> on construction.</param>
	/// <param name="name">The name of this recorder, for logging purposes.</param>
	public FeedRecorder(Func<FeedRecorder<TFeed, TValue>, TFeed> feedProvider, SourceContext context, bool autoEnable, string name)
	{
		_feedProvider = feedProvider;
		_context = context;
		Name = name;

		if (autoEnable)
		{
			Enable();
		}
	}

	/// <summary>
	/// The name of this recorder, for logging purposes.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The feed under test.
	/// </summary>
	public TFeed Feed
	{
		get
		{
			Enable();
			return _feed!;
		}
	}

	/// <summary>
	/// Deconstructs this recorder.
	/// </summary>
	/// <param name="result">This recorder.</param>
	/// <param name="feed">The <see cref="Feed"/>.</param>
	public void Deconstruct(out FeedRecorder<TFeed, TValue> result, out TFeed feed)
	{
		result = this;
		feed = Feed;
	}

	/// <summary>
	/// Enable the recorder. This will cause the reorder to subscribe to the <see cref="Feed"/>.
	/// </summary>
	public void Enable()
	{
		if (Interlocked.CompareExchange(ref _enumerationStarted, 1, 0) == 0)
		{
			_enumeration = (_feed = _feedProvider(this))
				.GetSource(_context, _ct.Token)
				.ForEachAsync(
					msg =>
					{
						_messages.Add(msg);
						_messageAdded?.Invoke(this, EventArgs.Empty);
					},
					_ct.Token);
		}
	}

	/// <inheritdoc />
	public async ValueTask WaitForEnd(int timeout, CancellationToken ct)
	{
		Enable();

		if (Debugger.IsAttached)
		{
			timeout *= 1000;
		}

		await Task.WhenAny(_enumeration!, Task.Delay(timeout, ct));

		if (!_enumeration!.IsCompleted)
		{
			throw new TimeoutException($"[{Name}] The source feed did not completed within the given delay of {TimeSpan.FromMilliseconds(timeout):g} ({_messages.Count} messages).");
		}

		await _enumeration; // Rethrows error if any.
	}

	/// <inheritdoc />
	public async ValueTask WaitForMessages(int count, int timeout, CancellationToken ct)
	{
		// Warning: This could be invoked by the _sourceProvider(), we must not use _enumeration nor _feed fields

		Enable();

		if (Debugger.IsAttached)
		{
			timeout *= 1000;
		}

		var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, new CancellationTokenSource(timeout).Token);
		var tcs = new TaskCompletionSource<Unit>(TaskCreationOptions.RunContinuationsAsynchronously); // Dissociate feed from test code to allow Feed to continue its work
		using (cts.Token.Register(() => tcs.TrySetCanceled()))
		{
			try
			{
				_messageAdded += OnMessageAdded;
				OnMessageAdded(this, EventArgs.Empty);
				await tcs.Task;
			}
			catch (OperationCanceledException) when (!ct.IsCancellationRequested && cts.IsCancellationRequested)
			{
				throw new TimeoutException($"[{Name}] The source feed did not produced the expected {count} messages (got only {_messages.Count}) within the given delay of {TimeSpan.FromMilliseconds(timeout):g}.\r\n{this}");
			}
			finally
			{
				_messageAdded -= OnMessageAdded;
			}
		}

		void OnMessageAdded(object? sender, EventArgs e)
		{
			if (_messages.Count >= count)
			{
				tcs.TrySetResult(default);
			}
		}
	}

	#region IReadOnlyList<Message<T>>
	/// <inheritdoc cref="ICollection" />
	public int Count => _messages.Count;

	/// <inheritdoc />
	public bool IsReadOnly => true;

	/// <inheritdoc />
	public Message<TValue> this[int index] => _messages[index];


	void ICollection<Message<TValue>>.Add(Message<TValue> item)
		=> throw NotSupported();

	bool ICollection<Message<TValue>>.Remove(Message<TValue> item)
		=> throw NotSupported();

	/// <inheritdoc />
	public void Clear()
		=> _messages.Clear();

	/// <inheritdoc />
	public bool Contains(Message<TValue> item)
		=> _messages.Contains(item);

	/// <inheritdoc />
	public void CopyTo(Message<TValue>[] array, int arrayIndex)
		=> _messages.CopyTo(array, arrayIndex);

	/// <inheritdoc />
	public IEnumerator<Message<TValue>> GetEnumerator()
		=> _messages.GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> ((IEnumerable)_messages).GetEnumerator();
	#endregion

	private static NotSupportedException NotSupported([CallerMemberName] string? operation = null)
		=> throw new NotSupportedException($"{operation} is not supported on MessageRecorder (read-only).");

	/// <inheritdoc />
	public void Dispose()
	{
		_ct.Cancel();
		Console.WriteLine(this);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		if (_feed is null)
		{
			return $"[FeedRecorder<{typeof(TValue).Name}>] {Name} ** NOT ENABLED **";
		}

		var sb = new StringBuilder();
		sb.AppendLine($"[FeedRecorder<{_feed.GetType().Name},{typeof(TValue).Name}>] {Name} ({_messages.Count} messages)");
		for (var i = 0; i < Count; i++)
		{
			var message = _messages[i];

			sb.Append($"\t{i + 1:D2}:");
			sb.Append($" data{HasChanged(MessageAxis.Data)}: {message.Current.Data}");
			sb.Append($" | error{HasChanged(MessageAxis.Error)}: {message.Current.Error?.GetType().Name ?? "None"}");
			sb.Append($" | progress{HasChanged(MessageAxis.Progress)}: {(message.Current.IsTransient ? "Transient" : "Final")}");

			foreach (var axis in message.Current.Values)
			{
				if (axis.Key == MessageAxis.Data || axis.Key == MessageAxis.Error || axis.Key == MessageAxis.Progress)
				{
					continue;
				}

				if (axis.Value.IsSet)
				{
					sb.Append($" | {axis.Key.Identifier}{HasChanged(axis.Key)}: {axis.Value.Value}");
				}
			}

			sb.AppendLine();

			foreach (var change in message.Changes)
			{
				message.Changes.Contains(change, out var changes);
				if (changes is not null && changes.ToString() is { } changesString)
				{
					sb.AppendLine($"\t\t{change.Identifier} changes:");
					foreach (var line in changesString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
					{
						sb.Append("\t\t\t");
						sb.AppendLine(line);
					}
				}
			}

			string HasChanged(MessageAxis axis)
				=> message.Changes.Contains(axis) ? "*" : "";
		}

		return sb.ToString();
	}
}
