using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

internal sealed class RequestSource : IRequestSource
{
	private bool _isDisposed;

	/// <inheritdoc />
	public event EventHandler<IContextRequest>? RequestRaised;

	/// <inheritdoc />
	public void Send(IContextRequest request)
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(nameof(RequestSource));
		}

		RequestRaised?.Invoke(this, request);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_isDisposed = true;
		RequestRaised?.Invoke(this, EndRequest.Instance);
		RequestRaised = null;
	}
}
