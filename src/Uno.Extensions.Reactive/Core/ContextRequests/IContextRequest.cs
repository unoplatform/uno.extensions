using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// Flag interface for a request that can be sent by a <see cref="SourceContext"/>.
/// </summary>
internal interface IContextRequest
{
}

internal class End : IContextRequest
{
	public static End Instance { get; } = new();

	private End()
	{
	}
}
