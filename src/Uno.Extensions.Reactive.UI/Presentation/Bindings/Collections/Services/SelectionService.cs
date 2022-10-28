using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Bindings.Collections.Services;

/// <summary>
/// A simple selection service which acts as a push-pull adapter between source and the <see cref="BindableCollection"/>.
/// </summary>
internal sealed class SelectionService : ISelectionService, IDisposable
{
	private readonly AsyncAction<uint?> _setSelectionFromView;

	private CancellationTokenSource? _setSelectionFromViewToken;
	private uint? _selectedIndex;
	private bool _isDisposed;

	/// <inheritdoc />
	public event EventHandler? StateChanged;


	public SelectionService(AsyncAction<uint?> setSelectionFromView)
	{
		_setSelectionFromView = setSelectionFromView;
	}

	/// <inheritdoc />
	public uint? SelectedIndex
	{
		get => _selectedIndex;
		private set
		{
			if (_selectedIndex != value)
			{
				_selectedIndex = value;
				StateChanged?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	/// <inheritdoc />
	public void SelectFromModel(uint? index)
		=> SelectedIndex = index;

	/// <inheritdoc />
	public void SelectFromView(int index)
	{
		var selectedIndex = SelectedIndex = index > 0 ? (uint?)index : null;

		if (_isDisposed)
		{
			return;
		}

		var ct = new CancellationTokenSource();
		Interlocked.Exchange(ref _setSelectionFromViewToken, ct)?.Cancel();

		if (_isDisposed)
		{
			ct.Cancel();
			return;
		}

		Task.Run(() => _setSelectionFromView(selectedIndex, ct.Token), ct.Token);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_isDisposed = true;
		_setSelectionFromViewToken?.Cancel();
	}
}
