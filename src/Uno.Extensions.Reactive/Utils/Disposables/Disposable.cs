using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Utils;

internal static class Disposable
{
	public static IDisposable Empty { get; } = new Null();

	private class Null : IDisposable
	{
		/// <inheritdoc />
		public void Dispose() { }
	}
}
