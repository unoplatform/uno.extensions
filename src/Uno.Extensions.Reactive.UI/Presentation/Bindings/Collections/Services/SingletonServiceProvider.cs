using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Bindings.Collections.Services;

internal class SingletonServiceProvider : IServiceProvider, IAsyncDisposable
{
	private object[] _services;

	public SingletonServiceProvider(params object[] services)
	{
		_services = services;
	}

	/// <inheritdoc />
	public object? GetService(Type serviceType)
		=> _services.FirstOrDefault(serviceType.IsInstanceOfType);

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		foreach (var service in Interlocked.Exchange(ref _services, Array.Empty<object>()))
		{
			switch (service)
			{
				case IAsyncDisposable asyncDisposable:
					await asyncDisposable.DisposeAsync().ConfigureAwait(false);
					break;

				case IDisposable disposable:
					disposable.Dispose();
					break;
			}
		}
	}
}
