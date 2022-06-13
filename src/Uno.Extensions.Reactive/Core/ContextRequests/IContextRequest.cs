using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// Flag interface for a request that can be sent by a <see cref="SourceContext"/>.
/// </summary>
internal interface IContextRequest
{
}

internal interface IContextRequest<in TToken> : IContextRequest
{
	void Register(TToken token);
}
