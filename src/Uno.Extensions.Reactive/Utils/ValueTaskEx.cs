using System;
using System.Linq;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Utils;

internal static class ValueTaskEx
{
	public static ValueTask WhenAll(params ValueTask[] tasks)
		=> new(Task.WhenAll(tasks.Select(t => t.AsTask())));
}
