using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive;

/// <summary>
/// An input from the view.
/// </summary>
/// <typeparam name="T">Type of the value of the input.</typeparam>
public interface IInput<T> : IState<T>
{
	/// <summary>
	/// The name of bindable property.
	/// </summary>
	public string PropertyName { get; }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IListInput<T> : IListState<T>
{
	/// <summary>
	/// The name of bindable property.
	/// </summary>
	public string PropertyName { get; }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
internal record ListInput<T>(IInput<IImmutableList<T>> _implementation) : ListState<T>(_implementation), IListInput<T>
{
	private readonly IInput<IImmutableList<T>> _implementation = _implementation;

	/// <inheritdoc />
	public string PropertyName => _implementation.PropertyName;
}


///// <summary/>
//public struct ListInput<T> : IInput<MyList<T>>
//{
//	private readonly IInput<MyList<T>> _implementation;

//	private ListInput(IInput<MyList<T>> implementation)
//	{
//		_implementation = implementation;
//	}

//	/// <inheritdoc />
//	public IAsyncEnumerable<Message<MyList<T>>> GetSource(SourceContext context, CancellationToken ct = default)
//		=> _implementation.GetSource(context, ct);

//	/// <inheritdoc />
//	public ValueTask Update(Func<Message<MyList<T>>, MessageBuilder<MyList<T>>> updater, CancellationToken ct)
//		=> _implementation.Update(updater, ct);

//	/// <inheritdoc />
//	public ValueTask DisposeAsync()
//		=> _implementation.DisposeAsync();

//	/// <inheritdoc />
//	public string PropertyName
//		=> _implementation.PropertyName;

//	/// <summary>
//	/// 
//	/// </summary>
//	/// <param name="implementation"></param>
//	/// <returns></returns>
//	public static implicit operator ListInput<T>(Input<MyList<T>> implementation)
//		=> new(implementation);

//	/// <summary>
//	/// 
//	/// </summary>
//	/// <param name="implementation"></param>
//	/// <returns></returns>
//	public static implicit operator Input<MyList<T>>(ListInput<T> implementation)
//		=> new(implementation);

//	/// <summary>
//	/// 
//	/// </summary>
//	/// <param name="implementation"></param>
//	/// <returns></returns>
//	public static implicit operator ListState<T>(ListInput<T> implementation)
//		=> new(implementation._implementation);


///// <summary>
///// 
///// </summary>
///// <typeparam name="T"></typeparam>
//public readonly struct Input<T> : IInput<T>
//{
//	private readonly IInput<T> _implementation;

//	internal Input(IInput<T> implementation)
//	{
//		_implementation = implementation is Input<T> input
//			? input._implementation
//			: implementation ?? throw new NullReferenceException(nameof(implementation));
//	}

//	/// <inheritdoc />
//	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
//		=> _implementation.GetSource(context, ct);

//	/// <inheritdoc />
//	public ValueTask Update(Func<Message<T>, MessageBuilder<T>> updater, CancellationToken ct)
//		=> _implementation.Update(updater, ct);

//	/// <inheritdoc />
//	public ValueTask DisposeAsync()
//		=> _implementation.DisposeAsync();

//	/// <inheritdoc />
//	public string PropertyName => _implementation.PropertyName;
//}
