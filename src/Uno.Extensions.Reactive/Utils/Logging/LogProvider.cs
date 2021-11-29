#pragma warning disable CS1591 // XML Doc, to be removed

using System;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Reactive.Logging;

public static class LogProvider
{
	public static void Set(ILoggerProvider provider)
		=> LogExtensions.SetProvider(provider);
}
