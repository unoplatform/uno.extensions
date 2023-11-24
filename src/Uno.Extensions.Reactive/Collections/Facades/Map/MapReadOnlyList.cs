using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.Extensions.Conversion;

namespace Uno.Extensions.Collections.Facades.Map;

/// <summary>
/// A facade over a readonly list which ensure the dynamic projection of items using an <see cref="IConverter{TFrom,TTo}"/>.
/// </summary>
/// <typeparam name="TFrom"></typeparam>
/// <typeparam name="TTo"></typeparam>
internal class MapReadOnlyList<TFrom, TTo> : IList, IList<TTo>, IReadOnlyList<TTo>
{
	private readonly IConverter<TFrom, TTo> _converter;
	private readonly IList _source;

	/// <summary>
	/// Creates a facade over a readonly list which ensure conversion of items using the provided converter.
	/// </summary>
	/// <param name="converter"></param>
	/// <param name="source">Must be read only itself</param>
	public MapReadOnlyList(IConverter<TFrom, TTo> converter, IList source)
	{
		_converter = converter;
		_source = source;

		if (!source.IsReadOnly)
		{
			throw new ArgumentException("MapReadOnlyList is only a facade over a list which should be read-only itself.", nameof(source));
		}
	}

	/// <inheritdoc />
	public int Count => _source.Count;

	/// <inheritdoc />
	public bool IsSynchronized => false;

	/// <inheritdoc />
	public object SyncRoot { get; } = new ();

	/// <inheritdoc />
	public bool IsReadOnly => true;

	/// <inheritdoc />
	public bool IsFixedSize => true;

	object? IList.this[int index]
	{
		get => _converter.Convert((TFrom)_source[index]!);
		set => throw NotSupported();
	}
	/// <inheritdoc />
	public TTo this[int index]
	{
		get => _converter.Convert((TFrom)_source[index]!);
		set => throw NotSupported();
	}

	#region Read
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	/// <inheritdoc />
	public IEnumerator<TTo> GetEnumerator() => new MapEnumerator<TFrom, TTo>(_converter, _source.GetEnumerator());

	/// <inheritdoc />
	public bool Contains(object? value) => value is TTo t && Contains(t);

	/// <inheritdoc />
	public bool Contains(TTo item)
	{
		var sourceItem = _converter.ConvertBack(item);
		return _source.IndexOf(sourceItem) >= 0;
	}

	/// <inheritdoc />
	public int IndexOf(object? value) => value is TTo t ? _source.IndexOf(_converter.ConvertBack(t)) : -1;
	/// <inheritdoc />
	public int IndexOf(TTo item) => _source.IndexOf(_converter.ConvertBack(item));

	/// <inheritdoc />
	public void CopyTo(TTo[] array, int arrayIndex) => _converter.ArrayCopy(_source, array, arrayIndex);
	/// <inheritdoc />
	public void CopyTo(Array array, int index) => _converter.ArrayCopy(_source, array, index);
	#endregion

	#region Write
	/// <inheritdoc />
	public void Add(TTo item) => throw NotSupported();

	/// <inheritdoc />
	public int Add(object? value) => throw NotSupported();

	/// <inheritdoc />
	public void Insert(int index, object? value) => throw NotSupported();

	/// <inheritdoc />
	public void Insert(int index, TTo item) => throw NotSupported();

	/// <inheritdoc />
	public void RemoveAt(int index) => throw NotSupported();

	/// <inheritdoc />
	public void Remove(object? value) => throw NotSupported();
	/// <inheritdoc />
	public bool Remove(TTo item) => throw NotSupported();

	/// <inheritdoc />
	public void Clear() => throw NotSupported();

	private NotSupportedException NotSupported([CallerMemberName] string? method = null)
		=> new($"{method} not supported on a read only list.");
	#endregion
}
