using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// This class is responsible to add advance support/features for FeedDependency for a given <see cref="FeedSession"/>.
/// FeedDependency managed by this class are however still pushed to the <see cref="FeedSession.Dependencies"/> for the default dependency management.
/// This class is also responsible to aggregate the last message of all the parent feeds (i.e. the de FeedDependency) and generate a <see cref="DynamicParentMessage"/> for those.
/// </summary>
internal sealed class FeedDependenciesStore : IDisposable
{
	private readonly Dictionary<IMessageEntry, FeedDependency> _dependenciesPerCurrentEntry = new();
	private readonly FeedSession _session;

	private bool _isDisposed;

	public FeedDependenciesStore(FeedSession session)
	{
		_session = session;
	}

	#region Feed.GetAwaiter support (resolution from the static FeedDependency class + axis touch tracking)
	private readonly Dictionary<ISignal<IMessage>, FeedDependency> _dependenciesPerFeed = new();

	public FeedDependency<T> Get<T>(IFeed<T> feed, FeedExecution execution)
	{
		Debug.Assert(execution.Session == _session);

		lock (_dependenciesPerFeed)
		{
			if (!_dependenciesPerFeed.TryGetValue(feed, out var dependency))
			{
				dependency = new FeedDependency<T>(execution, feed);
				_dependenciesPerFeed.Add(dependency.Feed, dependency);
				_session.RegisterDependency((IDependency)dependency);
			}

			return (FeedDependency<T>)dependency;
		}
	}

	public FeedDependency<IImmutableList<T>> Get<T>(IListFeed<T> listFeed, FeedExecution execution)
	{
		Debug.Assert(execution.Session == _session);

		lock (_dependenciesPerFeed)
		{
			if (!_dependenciesPerFeed.TryGetValue(listFeed, out var dependency))
			{
				dependency = new FeedDependency<IImmutableList<T>>(execution, listFeed);
				_dependenciesPerFeed.Add(dependency.Feed, dependency);
				_session.RegisterDependency((IDependency)dependency);
			}

			return (FeedDependency<IImmutableList<T>>)dependency;
		}
	}

	public bool TryGet(IMessageEntry entry, out FeedDependency dependency)
	{
		lock (_dependenciesPerCurrentEntry)
		{
			return _dependenciesPerCurrentEntry.TryGetValue(entry, out dependency);
		}
	}
	#endregion

	#region Support of Feed's last message caching / replay for a given session
	public void Update<T>(FeedDependency dependency, Message<T> last, bool updateEntryCache = false)
	{
		if (updateEntryCache)
		{
			lock (_dependenciesPerCurrentEntry)
			{
				_dependenciesPerCurrentEntry[last.Current] = dependency;
			}
		}

		UpdateParent(dependency.Feed, last);
	}

	public void Cleanup<T>(FeedDependency dependency, Message<T> last)
	{
		lock (_dependenciesPerCurrentEntry)
		{
			_dependenciesPerCurrentEntry.Remove(last.Current);
		}
	} 
	#endregion

	#region Parent message
	private readonly Dictionary<ISignal<IMessage>, IMessage> _parentMessages = new();
	private DynamicParentMessage _aggregatedParentMessage = DynamicParentMessage.Initial;
	private bool _isAggregatedParentMessageValid = true;

	private void UpdateParent(ISignal<IMessage> feed, IMessage message)
	{
		if (_isDisposed)
		{
			return;
		}

		if (_parentMessages.TryGetValue(feed, out var previous) && previous == message)
		{
			return;
		}

		lock (_parentMessages)
		{
			_isAggregatedParentMessageValid = false;
			_parentMessages[feed] = message;
		}

		_session.OnParentUpdated();
	}

	public IMessage GetParent()
	{
		lock (_parentMessages)
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(nameof(FeedSession), $"Cannot get parent message on a completed session.");
			}

			if (!_isAggregatedParentMessageValid)
			{
				_aggregatedParentMessage = _aggregatedParentMessage.With(_parentMessages.Values);
				_isAggregatedParentMessageValid = true;
			}

			return _aggregatedParentMessage;
		}
	}
	#endregion

	public void Remove(FeedDependency dependency)
	{
		// Note: We do not remove the dependency from the _dependenciesPerFeed: we will still use its final message until the end of the session!
		_session.UnRegisterDependency((IDependency)dependency);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_isDisposed = true;
	}
}
