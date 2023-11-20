using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Core;

internal sealed class CompositeRequestSource : IRequestSource
{
	private bool _isDisposed;
	private event EventHandler<IContextRequest>? _requestRaised;

	/// <summary>
	/// Adds a new source to this composite source.
	/// </summary>
	/// <param name="other">The source to add.</param>
	/// <param name="ct">A cancellation token that can be used to remove the given source.</param>
	public void Add(IRequestSource other, CancellationToken ct)
	{
		other.RequestRaised += OnRequestReceived;
		ct.Register(() => other.RequestRaised -= OnRequestReceived);

		void OnRequestReceived(object? _, IContextRequest request)
		{
			if (request is not EndRequest)
			{
				_requestRaised?.Invoke(null, request);
			}
		}
	}

	/// <inheritdoc />
	public event EventHandler<IContextRequest>? RequestRaised
	{
		add => _requestRaised += value;
		remove => _requestRaised -= value;
	}

	/// <inheritdoc />
	public void Send(IContextRequest request)
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(nameof(CompositeRequestSource));
		}
		_requestRaised?.Invoke(this, request);
	}

	/// <inheritdoc />
	public void Dispose()
	{
		_isDisposed = true;
		_requestRaised?.Invoke(this, EndRequest.Instance);
		_requestRaised = null;
	}
}
