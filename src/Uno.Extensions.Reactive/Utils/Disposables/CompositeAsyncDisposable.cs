using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

internal sealed class CompositeAsyncDisposable : ICollection<IAsyncDisposable>, IAsyncDisposable
{
	public static Task DisposeAll(params IAsyncDisposable[] disposables)
		=> Task.WhenAll(disposables.Select(d => d.DisposeAsync().AsTask()));

	public static Task DisposeAll(IEnumerable<IAsyncDisposable> disposables)
		=> Task.WhenAll(disposables.Select(d => d.DisposeAsync().AsTask()));

	private readonly List<IAsyncDisposable> _disposables = new();
	private int _isDisposed = 0;

	// Note: We don't do an explicit interface implementation for this one,
	//		 as it's pretty common to invoke the add a couple of times before on creation,
	//		 while it's impossible that this CompositeAsyncDisposable has been disposed.
	/// <inheritdoc />
	public void Add(IAsyncDisposable item) => _ = AddAsync(item);

	public ValueTask AddAsync(IAsyncDisposable item)
	{
		if (_isDisposed == 1)
		{
			return item.DisposeAsync();
		}

		lock (_disposables)
		{
			if (_isDisposed == 1)
			{
				return item.DisposeAsync();
			}
				
			_disposables.Add(item);

			return default;
		}
	}

	/// <inheritdoc />
	public bool Remove(IAsyncDisposable item) => RemoveCore(item).isRemoved;

	public async ValueTask<bool> RemoveAsync(IAsyncDisposable item)
	{
		var (isRemoved, asyncDispose) = RemoveCore(item);
		if (isRemoved)
		{
			await asyncDispose.ConfigureAwait(false);
			return true;
		}
		else
		{
			return false;
		}
	}

	public (bool isRemoved, ValueTask asyncDispose) RemoveCore(IAsyncDisposable item)
	{
		bool isRemoved;
		lock (_disposables)
		{
			isRemoved = _disposables.Remove(item);
		}

		if (isRemoved)
		{
			return (true, item.DisposeAsync());
		}
		else
		{
			return (false, default);
		}
	}

	/// <inheritdoc />
	public void Clear() => _ = ClearAsync();

	public ValueTask ClearAsync()
	{
		List<IAsyncDisposable> disposables;
		lock (_disposables)
		{
			disposables = _disposables.ToList();
			_disposables.Clear();
		}

		return new(DisposeAll(disposables));
	}

	#region ICollection<IAsyncDisposable>
	/// <inheritdoc />
	public IEnumerator<IAsyncDisposable> GetEnumerator()
		=> _disposables.GetEnumerator();

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> ((IEnumerable)_disposables).GetEnumerator();

	/// <inheritdoc />
	public bool Contains(IAsyncDisposable item)
		=> _disposables.Contains(item);

	/// <inheritdoc />
	public void CopyTo(IAsyncDisposable[] array, int arrayIndex)
		=> _disposables.CopyTo(array, arrayIndex);

	/// <inheritdoc />
	public int Count => _disposables.Count;

	/// <inheritdoc />
	public bool IsReadOnly => ((ICollection<IAsyncDisposable>)_disposables).IsReadOnly;
	#endregion

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
		{
			lock (_disposables)
			{
				return new(DisposeAll(_disposables));
			}
		}
		else
		{
			return default;
		}
	}
}
