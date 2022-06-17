using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// A source of <see cref="IContextRequest"/> that can be used with a <see cref="SourceContext"/>.
/// </summary>
internal interface IRequestSource : IDisposable
{
	public event EventHandler<IContextRequest> RequestRaised;

	/// <summary>
	/// Send a new request.
	/// </summary>
	/// <param name="request">The request to send.</param>
	public void Send(IContextRequest request);
}
