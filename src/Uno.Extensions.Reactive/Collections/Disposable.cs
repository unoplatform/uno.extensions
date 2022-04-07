using System;
using System.Linq;

namespace Umbrella.Reactive.Collections;

internal static class Disposable
{
	public static IDisposable Empty { get; } = new Null();

	private class Null : IDisposable
	{
		/// <inheritdoc />
		public void Dispose() { }
	}
}
