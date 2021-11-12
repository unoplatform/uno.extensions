using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Utils;

internal static class AsyncOperationManager
{
	public static IAsyncOperationsManager Create(ConcurrencyMode mode, bool silentErrors = false)
		=> mode switch
		{
			ConcurrencyMode.Queue => new SequentialAsyncOperationsManager(silentErrors),
			ConcurrencyMode.AbortPrevious => new LastWinsAsyncOperationManager(silentErrors),
			ConcurrencyMode.IgnoreNew => new FirstWinsAsyncOperationManager(silentErrors),
			ConcurrencyMode.Parallel => new ConcurrentAsyncOperationManager(silentErrors),
			_ => throw new NotSupportedException($"Concurrency mode '{mode}' is not supported.")
		};
}
