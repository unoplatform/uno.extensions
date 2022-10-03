using System;
using System.Linq;
using Uno.Toolkit;

namespace Uno.Extensions.Reactive.UI;

partial class FeedView : ILoadable
{
	private bool _isLoading = true; // True by default
	private event EventHandler? _isLoadingChanged;

	/// <inheritdoc />
	event EventHandler? ILoadable.IsExecutingChanged
	{
		add => _isLoadingChanged += value;
		remove => _isLoadingChanged -= value;
	}

	/// <inheritdoc />
	bool ILoadable.IsExecuting => _isLoading;

	private void SetIsLoading(bool isLoading)
	{
		if (_isLoading != isLoading)
		{
			_isLoading = isLoading;
			_isLoadingChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
