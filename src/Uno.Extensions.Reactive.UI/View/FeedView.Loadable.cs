using System;
using System.Linq;
using Uno.Toolkit;

namespace Uno.Extensions.Reactive.UI;

partial class FeedView : ILoadable, ISmoothVisualStateAware
{
	private bool _isLoading = true; // True by default, so we are considered as loading even before the source is being set.
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

	// If we are loading and there is listener for the IsLoadingChanged event, it usually means that we have a parent LoadingView,
	// so we should go to the state synchronously to avoid flicker between loading states of the LoadingView and the FeedView.
	/// <inheritdoc />
	bool ISmoothVisualStateAware.ShouldGoToStateSync => _isLoading && _isLoadingChanged is not null;
}
