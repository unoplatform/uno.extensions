using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Uno.Extensions.Reactive.Utils.Debugging;

internal class DebugConfiguration
{
	private static bool? _isDebugging;

	public static bool IsDebugging => _isDebugging ??= Debugger.IsAttached | Logging.LogExtensions.Log<ISignal>().IsEnabled(LogLevel.Debug);
}
