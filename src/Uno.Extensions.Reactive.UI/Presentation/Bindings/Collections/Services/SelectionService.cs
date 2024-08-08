#pragma warning disable Uno0001 // ISelectionInfo is only an interface!

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Bindings.Collections.Services;

/// <summary>
/// A simple selection service which acts as a push-pull adapter between source and the <see cref="BindableCollection"/>.
/// </summary>
internal sealed class SelectionService : ISelectionService, IDisposable, ISelectionInfo
{
	private readonly AsyncAction<SelectionInfo> _setSelectionFromView;

	private CancellationTokenSource? _setSelectionFromViewToken;
	private SelectionInfo _selection = SelectionInfo.Empty;
	private bool _isDisposed;

	/// <inheritdoc />
	public event EventHandler? StateChanged;

	public SelectionService(AsyncAction<SelectionInfo> setSelectionFromView)
	{
		_setSelectionFromView = setSelectionFromView;
	}

	#region Change selection from source
	public void SetFromSource(SelectionInfo selection)
	{
		if (Update((_, s) => s, selection))
		{
			StateChanged?.Invoke(this, EventArgs.Empty);
		}
	} 
	#endregion

	#region Modify selection from View
	/// <inheritdoc />
	public bool IsSelected(int index)
		=> _selection.Contains(index);

	/// <inheritdoc />
	public IReadOnlyList<ItemIndexRange> GetSelectedRanges()
		=> _selection.Ranges.Select(range => new ItemIndexRange((int)range.FirstIndex, range.Length)).ToList();

	/// <inheritdoc />
	public void SelectRange(ItemIndexRange itemIndexRange)
	{
		if (Update((info, added) => info.Add(added), new SelectionIndexRange((uint)itemIndexRange.FirstIndex, itemIndexRange.Length)))
		{
			PushToSource();
		}
	}

	/// <inheritdoc />
	public void ReplaceRange(ItemIndexRange itemIndexRange)
	{
		if (Update((_, updated) => SelectionInfo.Empty.Add(updated), new SelectionIndexRange((uint)itemIndexRange.FirstIndex, itemIndexRange.Length)))
		{
			PushToSource();
		}
	}

	/// <inheritdoc />
	public void DeselectRange(ItemIndexRange itemIndexRange)
	{
		if (Update((info, removed) => info.Remove(removed), new SelectionIndexRange((uint)itemIndexRange.FirstIndex, itemIndexRange.Length)))
		{
			PushToSource();
		}
	} 
	#endregion

	private bool Update<T>(Func<SelectionInfo, T, SelectionInfo> update, T arg)
	{
		while (true)
		{
			var original = _selection;
			var updated = update(original, arg);

			if (original == updated)
			{
				return false;
			}

			if (Interlocked.CompareExchange(ref _selection, updated, original) == original)
			{
				return true;
			}
		}
	}

	private void PushToSource()
	{
		var ct = new CancellationTokenSource();
		Interlocked.Exchange(ref _setSelectionFromViewToken, ct)?.Cancel();

		if (_isDisposed)
		{
			ct.Cancel();
			return;
		}

		Task.Run(() => _setSelectionFromView(_selection, ct.Token), ct.Token);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_isDisposed = true;
		_setSelectionFromViewToken?.Cancel();
	}
}
