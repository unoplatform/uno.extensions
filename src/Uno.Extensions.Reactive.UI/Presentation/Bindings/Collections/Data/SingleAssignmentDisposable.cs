using System;
using System.Linq;
using System.Threading;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data;

internal class SingleAssignmentDisposable : IDisposable
{
	private IDisposable? _disposable;
	private int _isSet;

	public IDisposable? Disposable
	{
		get => _disposable;
		set
		{
			if (Interlocked.CompareExchange(ref _isSet, 1, 0) is not 0)
			{
				throw new InvalidOperationException("Disposable already set.");
			}

			if (Interlocked.CompareExchange(ref _disposable, value, null) is not null)
			{
				value?.Dispose();
			}
		}
	}

	/// <inheritdoc />
	public void Dispose()
		=> Interlocked.Exchange(ref _disposable, Uno.Extensions.Reactive.Utils.Disposable.Empty)?.Dispose();
}
