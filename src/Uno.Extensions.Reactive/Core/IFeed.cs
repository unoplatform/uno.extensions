using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A **stateless** stream of data.
/// </summary>
/// <typeparam name="T">The type of the data</typeparam>
public interface IFeed<T> : ISignal<Message<T>>
	/* where T : record */
{
}

internal interface IListFeedWrapper<T>
{
	IFeed<IImmutableList<T>> Source { get; }
}

public interface IListFeed<T> : ISignal<Message<IImmutableList<T>>>
{
}

public interface IListState<T> : IListFeed<T>
{
	ValueTask Update(Func<Message<IImmutableList<T>>, MessageBuilder<IImmutableList<T>>> updater, CancellationToken ct);
}



//public readonly struct TestFeed<T> : IFeed<T>
//{
//	private readonly IFeed<T> _implementation;

//	public TestFeed(IFeed<T> implementation)
//	{
//		_implementation = implementation;
//	}

//	/// <inheritdoc />
//	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
//		=> _implementation.GetSource(context, ct);
//}

/////// <summary/>
////public interface IListFeed<T> : IFeed<MyList<T>>
////{
////}


///// <summary>
///// 
///// </summary>
///// <typeparam name="T"></typeparam>
//public readonly struct ListFeed<T> : IFeed<MyList<T>>
//{
//	private readonly IFeed<MyList<T>> _implementation;

//	/// <summary>
//	/// 
//	/// </summary>
//	/// <param name="implementation"></param>
//	public ListFeed(IFeed<MyList<T>> implementation)
//	{
//		_implementation = implementation;
//	}

//	/// <inheritdoc />
//	public IAsyncEnumerable<Message<MyList<T>>> GetSource(SourceContext context, CancellationToken ct = default)
//		=> _implementation.GetSource(context, ct);


//	public static implicit operator ListFeed<T>(TestFeed<MyList<T>> implemenation)
//		=> new(implemenation);
//}

//public readonly struct ListFeed<TCollection, TItem> : IFeed<TCollection>
//	where TCollection : IImmutableList<TItem>
//{
//	private readonly IFeed<TCollection> _implementation;

//	public ListFeed(IFeed<TCollection> implementation)
//	{
//		_implementation = implementation;
//	}

//	/// <inheritdoc />
//	public IAsyncEnumerable<Message<TCollection>> GetSource(SourceContext context, CancellationToken ct = default)
//		=> _implementation.GetSource(context, ct);


//	public static implicit operator ListFeed<TCollection, TItem>(TestFeed<TCollection> implemenation)
//		=> new(implemenation);
//}

//public static class ListFeed
//{
//	public static ListFeed<T> Async<T>(AsyncFunc<IEnumerable<T>> valueProvider)
//		=> new(AttachedProperty.GetOrCreate(valueProvider, vp => new AsyncFeed<MyList<T>>(async ct => new MyList<T>((await vp(ct)).ToImmutableList()))));
//}

///// <summary/>
//public readonly struct ListState<T> : IState<MyList<T>>
//{
//	private readonly IState<MyList<T>> _implementation;

//	/// <summary>
//	/// 
//	/// </summary>
//	/// <param name="implementation"></param>
//	/// <exception cref="NullReferenceException"></exception>
//	public ListState(IState<MyList<T>> implementation)
//	{
//		_implementation = implementation is ListState<T> state
//			? state._implementation
//			: implementation ?? throw new NullReferenceException(nameof(implementation));
//	}

//	/// <inheritdoc />
//	IAsyncEnumerable<Message<MyList<T>>> IState<MyList<T>>.GetSource(SourceContext context, CancellationToken ct)
//		=> _implementation.GetSource(context, ct);

//	/// <inheritdoc />
//	ValueTask IState<MyList<T>>.Update(Func<Message<MyList<T>>, MessageBuilder<MyList<T>>> updater, CancellationToken ct)
//		=> _implementation.Update(updater, ct);

//	/// <inheritdoc />
//	public ValueTask DisposeAsync()
//		=> _implementation.DisposeAsync();
//}

///// <summary>
///// 
///// </summary>
///// <typeparam name="TCollection"></typeparam>
///// <typeparam name="TItem"></typeparam>
//public readonly struct ListState<TCollection, TItem> : IState<TCollection>
//	where TCollection : IImmutableList<TItem>
//{
//	private readonly IState<TCollection> _implementation;

//	/// <summary>
//	/// 
//	/// </summary>
//	/// <param name="implementation"></param>
//	public ListState(IState<TCollection> implementation)
//	{
//		_implementation = implementation;
//	}

//	/// <inheritdoc />
//	public IAsyncEnumerable<Message<TCollection>> GetSource(SourceContext context, CancellationToken ct = default)
//		=> _implementation.GetSource(context, ct);

//	/// <inheritdoc />
//	public ValueTask Update(Func<Message<TCollection>, MessageBuilder<TCollection>> updater, CancellationToken ct)
//		=> _implementation.Update(updater, ct);

//	/// <inheritdoc />
//	public ValueTask DisposeAsync()
//		=> _implementation.DisposeAsync();
//}

///// <inheritdoc />
//public class MyList<T> : IImmutableList<T>
//{
//	public MyList(IImmutableList<T> inner)
//	{
		
//	}

//	/// <inheritdoc />
//	public IEnumerator<T> GetEnumerator()
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	IEnumerator IEnumerable.GetEnumerator()
//		=> GetEnumerator();

//	/// <inheritdoc />
//	public int Count { get; }

//	/// <inheritdoc />
//	public T this[int index] => throw new NotImplementedException();

//	/// <inheritdoc />
//	public IImmutableList<T> Clear()
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	public int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	public int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer)
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	IImmutableList<T> IImmutableList<T>.Add(T value) => null!;

//	//void IList<T>.Add(T value) { }

//	//IImmutableList<T> IImmutableList<T>.Add(T value)
//	//	=> throw new NotImplementedException();
//	/// <summary>
//	/// 
//	/// </summary>
//	/// <param name="value"></param>
//	/// <returns></returns>
//	/// <exception cref="NotImplementedException"></exception>
//	public MyList<T> Add(T value)
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	public IImmutableList<T> AddRange(IEnumerable<T> items)
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	public IImmutableList<T> Insert(int index, T element)
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	public IImmutableList<T> InsertRange(int index, IEnumerable<T> items)
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	public IImmutableList<T> Remove(T value, IEqualityComparer<T>? equalityComparer)
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	public IImmutableList<T> RemoveAll(Predicate<T> match)
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	public IImmutableList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer)
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	public IImmutableList<T> RemoveRange(int index, int count)
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	public IImmutableList<T> RemoveAt(int index)
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	public IImmutableList<T> SetItem(int index, T value)
//		=> throw new NotImplementedException();

//	/// <inheritdoc />
//	public IImmutableList<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer)
//		=> throw new NotImplementedException();
//}
