using System;
using System.Linq;
using System.Threading;

namespace Uno.Extensions.Reactive.Utils;

internal static class Disposable
{
	public static IDisposable Empty { get; } = new Null();

	private class Null : IDisposable
	{
		/// <inheritdoc />
		public void Dispose() { }
	}

	public static IDisposable Create(Action disposeAction)
		=> new ActionDisposable(disposeAction);

	private class ActionDisposable : IDisposable
	{
		private Action? _disposeAction;

		public ActionDisposable(Action disposeAction)
		{
			_disposeAction = disposeAction;
		}

		/// <inheritdoc />
		public void Dispose()
			=> Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
	}
}
