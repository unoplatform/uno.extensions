using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Collections.Facades.Slice;

internal class SliceList : IList
{
	private readonly IList _source;
	private readonly int _startIndex;

	public SliceList(IList source, int startIndex, int count)
	{
		Count = count;
		_source = source;
		_startIndex = startIndex;
	}

	/// <inheritdoc />
	public int Count { get; }

	/// <inheritdoc />
	public bool IsSynchronized => _source.IsSynchronized;

	/// <inheritdoc />
	public object SyncRoot => _source.SyncRoot;

	/// <inheritdoc />
	public bool IsFixedSize => true;

	/// <inheritdoc />
	public bool IsReadOnly => true;

	/// <inheritdoc />
	public object? this[int index]
	{
		get => _source[index];
		set => throw NotSupported();
	}

	/// <inheritdoc />
	public int Add(object? value)
		=> throw NotSupported();

	/// <inheritdoc />
	public void Clear()
		=> throw NotSupported();

	/// <inheritdoc />
	public bool Contains(object? value)
		=> IndexOf(value) >= 0;

	/// <inheritdoc />
	public int IndexOf(object? value)
		=> _source.IndexOf(value, _startIndex, Count, null);

	/// <inheritdoc />
	public void Insert(int index, object? value)
		=> throw NotSupported();

	/// <inheritdoc />
	public void Remove(object? value)
		=> throw NotSupported();

	/// <inheritdoc />
	public void RemoveAt(int index)
		=> throw NotSupported();

	/// <inheritdoc />
	public IEnumerator GetEnumerator()
		=> new SliceEnumerator(_source.GetEnumerator(), _startIndex, Count);

	/// <inheritdoc />
	public void CopyTo(Array array, int index)
	{
		var tmp = new object[_source.Count];
		_source.CopyTo(tmp, 0);
		Array.Copy(tmp, _startIndex, array, index, Count);
	}

	private NotSupportedException NotSupported([CallerMemberName] string? name = null)
		=> new(name + " is not supported on a ReadOnly list");
}
