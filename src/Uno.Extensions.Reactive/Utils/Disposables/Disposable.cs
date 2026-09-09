using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Utils;

internal static class Disposable
{
	public static IDisposable Empty { get; } = new Null();

	public static IDisposable Create(Action onDispose) => new Anonymous(onDispose);

	private class Null : IDisposable
	{
		/// <inheritdoc />
		public void Dispose() { }
	}

	private sealed class Anonymous : IDisposable
	{
		private Action? _onDispose;

		public Anonymous(Action onDispose) => _onDispose = onDispose;

		/// <inheritdoc />
		public void Dispose() => System.Threading.Interlocked.Exchange(ref _onDispose, null)?.Invoke();
	}
}
