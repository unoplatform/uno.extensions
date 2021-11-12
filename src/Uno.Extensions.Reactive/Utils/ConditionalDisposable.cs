using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.Extensions.Reactive.Utils.Logging;

namespace Uno.Extensions.Reactive;

internal sealed class ConditionalDisposable
{
	private static readonly ConditionalWeakTable<object, Handle> _stores = new();

	public static void Link(object owner, IDisposable disposable)
		=> _stores.GetOrCreateValue(owner).Add(disposable);

	public static void Link(object owner, IAsyncDisposable disposable)
		=> _stores.GetOrCreateValue(owner).Add(disposable);

	private class Handle
	{
		private readonly List<object> _disposables = new();

		public void Add(IDisposable disposable)
			=> _disposables.Add(disposable);

		public void Add(IAsyncDisposable disposable)
			=> _disposables.Add(disposable);

		~Handle()
		{
			foreach (var disposable in _disposables)
			{
				try
				{
					switch (disposable)
					{
						case IDisposable syncDisposable:
							syncDisposable.Dispose();
							break;
						case IAsyncDisposable asyncDisposable:
							asyncDisposable.DisposeAsync();
							break;
					}
				}
				catch (Exception error)
				{
					this.Log().Error(error, "Got an exception in dispose.");
				}
			}
		}
	}
}
