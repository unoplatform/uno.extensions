using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive.UI;

public partial class FeedView
{
	internal class RefreshCommand : IAsyncCommand
	{
		private static readonly PropertyChangedEventArgs _isExecutingChanged = new(nameof(IsExecuting));

		/// <inheritdoc />
		public event EventHandler? CanExecuteChanged;

		/// <inheritdoc />
		public event PropertyChangedEventHandler? PropertyChanged;

		private readonly FeedView _view;
		private bool _isExecuting;

		public RefreshCommand(FeedView view)
		{
			_view = view;
		}

		/// <inheritdoc />
		public bool IsExecuting
		{
			get => _isExecuting;
			internal set
			{
				if (value != _isExecuting)
				{
					_isExecuting = value;
					CanExecuteChanged?.Invoke(this, EventArgs.Empty);
					PropertyChanged?.Invoke(this, _isExecutingChanged);
				}
			}
		}

		/// <inheritdoc />
		public bool CanExecute(object? parameter)
			=> !IsExecuting;

		/// <inheritdoc />
		public void Execute(object? parameter)
			=> IsExecuting = _view._subscription?.RequestRefresh() ?? false;
	}
}
