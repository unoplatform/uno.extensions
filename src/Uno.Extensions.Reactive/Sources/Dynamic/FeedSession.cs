using System;
using System.Collections.Immutable;
using System.Linq;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Operators;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// Represents an enumeration session of a <see cref="DynamicFeed{T}"/> for a given <see cref="SourceContext"/>.
/// This is created each time you <see cref="ISignal{T}.GetSource"/> of a DynamicFeed.
/// </summary>
internal abstract class FeedSession // Interface that is a concrete class as it must not be implemented by devs
{
	// Debug: event EventHandler<FeedAsyncExecution>? ExecutionStarted;

	private protected FeedSession(ISignal<IMessage> owningSignal, SourceContext context, CancellationToken ct)
	{
		Owner = owningSignal;
		Context = context;
		Token = ct;
	}

	/// <summary>
	/// The context for which this sessions is created.
	/// </summary>
	public SourceContext Context { get; }

	/// <summary>
	/// A cancellation token that is linked to the life time of this session.
	/// </summary>
	/// <remarks>
	/// This token will be cancelled at the end of the current session while the <see cref="SourceContext.Token"/>
	/// is linked to the lifetime of the owning source context and will remain active across sessions!
	/// </remarks>
	public CancellationToken Token { get; }

	/// <summary>
	/// The signal which created this session.
	/// </summary>
	public ISignal<IMessage> Owner { get; }

	/// <summary>
	/// Requests to start a new execution.
	/// </summary>
	/// <param name="request">The execution request.</param>
	/// <returns></returns>
	public abstract void Execute(ExecuteRequest request);

	/// <summary>
	/// Allows a ****FeedDependency**** to add a parent message.
	/// </summary>
	internal abstract void UpdateParent(ISignal<IMessage> feed, IMessage message);

	#region Dependencies support (including parent feeds dependencies)
	private ImmutableList<IDependency> _dependencies = ImmutableList<IDependency>.Empty;
	private FeedDependenciesStore? _feedDependencies;

	/// <summary>
	/// The feed on which the <see cref="Owner"/> is dependent upon.
	/// </summary>
	internal FeedDependenciesStore Feeds
	{
		get
		{
			// As lot of feed might not have feeds dependencies, we init store lazily
			if (_feedDependencies is null)
			{
				Interlocked.CompareExchange(ref _feedDependencies, new(this), null);
			}
			return _feedDependencies;
		}
	}

	/// <summary>
	/// Gets the list of the currently registered dependencies.
	/// </summary>
	public IImmutableList<IDependency> Dependencies => _dependencies;

	/// <summary>
	/// Registers a dependency for the current session.
	/// </summary>
	/// <param name="dependency">A dependency that can trigger a <see cref="Execute"/>.</param>
	public void RegisterDependency(IDependency dependency)
		=> ImmutableInterlocked.Update(ref _dependencies, static (list, item) => list.Add(item), dependency);

	/// <summary>
	/// Un-registers a dependency from the current session.
	/// </summary>
	/// <param name="dependency">The dependency to remove.</param>
	/// <remarks>Removing the last dependency will cause teh session to complete (or at the end of the current execution).</remarks>
	public void UnRegisterDependency(IDependency dependency)
		=> ImmutableInterlocked.Update(ref _dependencies, static (list, item) => list.Remove(item), dependency);
	#endregion

	#region Session lifetime objects (extensions helper, no behavior impact)
	private readonly Dictionary<object, object> _sharedInstances = new();

	/// <summary>
	/// Sets an object that will be shared across all executions of the current session.
	/// </summary>
	/// <typeparam name="TKey">Type of the key</typeparam>
	/// <typeparam name="TValue">Type of the shared object</typeparam>
	/// <param name="key">The key that identifies the value. It has to be unique between all dependencies.</param>
	/// <param name="value">The shared instance.</param>
	public void SetShared<TKey, TValue>(TKey key, TValue value)
		where TKey : notnull
		where TValue : notnull
	{
		lock (_sharedInstances)
		{
			_sharedInstances[key] = value;
		}
	}

	/// <summary>
	/// Gets or create an object that will be shared across all executions of the current session.
	/// </summary>
	/// <typeparam name="TKey">Type of the key</typeparam>
	/// <typeparam name="TArgs">Type of the arguments for the <paramref name="factory"/>.</typeparam>
	/// <typeparam name="TValue">Type of the shared object</typeparam>
	/// <param name="key">The key that identifies the value. It has to be unique between all dependencies.</param>
	/// <param name="factory">Factory to create the object if missing.</param>
	/// <param name="args">The arguments for the <paramref name="factory"/>.</param>
	public TValue GetShared<TKey, TArgs, TValue>(TKey key, Func<FeedSession, TKey, TArgs, TValue> factory, TArgs args)
		where TKey : notnull
		where TValue : notnull
	{
		lock (_sharedInstances)
		{
			if (!_sharedInstances.TryGetValue(key, out var value))
			{
				_sharedInstances[key] = value = factory(this, key, args);
			}

			return (TValue)value;
		}
	} 
	#endregion

}
