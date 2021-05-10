using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Chinook.DynamicMvvm
{
	public static class ViewModelExtensions2
	{
		public static TDisposable GetOrCreateDisposable<TDisposable>(this IViewModel vm, Func<TDisposable> create, [CallerMemberName] string key = null)
			where TDisposable : IDisposable
		{
			if (vm is null)
			{
				throw new ArgumentNullException(nameof(vm));
			}

			if (create is null)
			{
				throw new ArgumentNullException(nameof(create));
			}

			if (vm.TryGetDisposable(key, out var existingDisposable))
			{
				return (TDisposable)existingDisposable;
			}
			else
			{
				var disposable = create();
				vm.AddDisposable(key, disposable);
				return disposable;
			}
		}
	}
}
