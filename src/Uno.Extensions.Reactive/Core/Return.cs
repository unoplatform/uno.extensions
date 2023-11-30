using System;
using System.Linq;

namespace Uno.Extensions.Reactive;

public static class Return
{
	public static None<T> None<T>()
		where T : notnull
		=> default;
}

public readonly struct None<T>
	where T : notnull
{
	public static implicit operator T(None<T> _)
	{
		//FeedMethodBuilder<T>.Current
		return default!;
	}
}
