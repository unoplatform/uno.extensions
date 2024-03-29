﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Core;

internal sealed class NoneRequestSource : IRequestSource
{
	/// <inheritdoc />
	public event EventHandler<IContextRequest>? RequestRaised
	{
		add => value?.Invoke(this, EndRequest.Instance);
		remove { }
	}

	/// <inheritdoc />
	public void Send(IContextRequest request)
	{
		if (this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"Attempt to send a {request?.GetType()} message on the None SourceContext");
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
	}
}
