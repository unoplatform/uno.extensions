using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// A request that can be sent by a <see cref="SourceContext"/> that supports propagation tracking.
/// </summary>
internal interface IContextRequest<in TToken> : IContextRequest
{
	/// <summary>
	/// Registers a response token.
	/// A token is emitted by each signal that will produce new messages in response to this request.
	/// </summary>
	/// <param name="token">The token to register.</param>
	void Register(TToken token);
}
