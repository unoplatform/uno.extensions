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

public class FeedRecorder<TFeed, TValue> : IFeedRecorder<TValue>, ICollection<Message<TValue>>, IDisposable
	where TFeed : IFeed<TValue>
{
	private event EventHandler? _messageAdded;

	private readonly Func<FeedRecorder<TFeed, TValue>, TFeed> _sourceProvider;
	private readonly SourceContext _context;
	private readonly CancellationTokenSource _ct = new();
	private readonly List<Message<TValue>> _messages = new();

	private int _enumerationStarted;
	private Task? _enumeration;
	private TFeed? _feed;

	public string Name { get; }

	public FeedRecorder(Func<FeedRecorder<TFeed, TValue>, TFeed> sourceProvider, SourceContext context, bool autoEnable, string name)
	{
		_sourceProvider = sourceProvider;
		_context = context;
		Name = name;

		if (autoEnable)
		{
			Enable();
		}
	}

	public TFeed Feed
	{
		get
		{
			Enable();
			return _feed!;
		}
	}

	public void Enable()
	{
		if (Interlocked.CompareExchange(ref _enumerationStarted, 1, 0) == 0)
		{
			_enumeration = (_feed = _sourceProvider(this))
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

	public TaskAwaiter<FeedRecorder<TFeed, TValue>> GetAwaiter()
	{
		Enable();

		var opts = TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.AttachedToParent // Prefer to run sync as we only want to propagate this
			| TaskContinuationOptions.OnlyOnRanToCompletion // If an error is thrown, we don't need to send back this ^^
			| TaskContinuationOptions.RunContinuationsAsynchronously | TaskContinuationOptions.DenyChildAttach; // Dissociate feed from test code to allow Feed to continue its work
		var awaiter = _enumeration!
			.ContinueWith((_, state) => (FeedRecorder<TFeed, TValue>)state!, this, opts)
			.GetAwaiter();

		return awaiter;
	}

	public void Deconstruct(out FeedRecorder<TFeed, TValue> result, out TFeed feed)
	{
		result = this;
		feed = Feed;
	}

	public async ValueTask WaitForMessages(int count, CancellationToken ct, int timeout = 1000)
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
			finally
			{
				_messageAdded -= OnMessageAdded;
			}
		}

		void OnMessageAdded(object sender, EventArgs e)
		{
			if (_messages.Count >= count)
			{
				tcs.TrySetResult(default);
			}
		}
	}

	#region IReadOnlyList<Message<T>>
	/// <inheritdoc />
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

			string HasChanged(MessageAxis axis)
				=> message.Changes.Contains(axis) ? "*" : "";
		}

		return sb.ToString();
	}
}
