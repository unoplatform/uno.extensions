using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

internal sealed class FeedDependenciesStore
{
	private readonly Dictionary<IMessageEntry, FeedDependency> _dependenciesPerCurrentEntry = new();
	private readonly Dictionary<ISignal<IMessage>, FeedDependency> _dependenciesPerFeed = new();
	private readonly FeedSession _session;

	public FeedDependenciesStore(FeedSession session)
	{
		_session = session;
	}

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

	public void Update<T>(FeedDependency dependency, Message<T> last, bool updateEntryCache = false)
	{
		if (updateEntryCache)
		{
			lock (_dependenciesPerCurrentEntry)
			{
				_dependenciesPerCurrentEntry[last.Current] = dependency; 
			}
		}
		_session.UpdateParent(dependency.Feed, last);
	}

	public void CleanupCache<T>(FeedDependency dependency, Message<T> last)
	{
		lock (_dependenciesPerCurrentEntry)
		{
			_dependenciesPerCurrentEntry.Remove(last.Current);
		}
	}

	public void Cleanup(FeedDependency dependency)
	{
		// Note: We do not remove the dependency from the _dependenciesPerFeed: we will still use its final message until the end of teh session!
		_session.UnRegisterDependency((IDependency)dependency);
	}
}
