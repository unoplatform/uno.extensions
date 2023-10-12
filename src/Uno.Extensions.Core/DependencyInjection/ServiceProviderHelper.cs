using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Extensions.DependencyInjection;

/// <summary>
/// Set of helpers for <see cref="IServiceProvider"/>.
/// </summary>
public static class ServiceProviderHelper
{
	private static readonly ConditionalWeakTable<object, IServiceProvider> _providers = new();

	/// <summary>
	/// Weakly attaches a service provider to an object.
	/// This allows to retrieve the service provider used to create the object instance later, like for hot-reload purposes.
	/// </summary>
	/// <param name="provider"></param>
	/// <param name="owner"></param>
	public static void SetProvider(this IServiceProvider provider, object owner)
		=> _providers.Add(owner, provider);

	/// <summary>
	/// Attempts to get the service provider attached to an object.
	/// 
	/// WARNING: A good practice is to **not use** the returned service provider for normal service resolution,
	/// but only for dynamic best-effort scenario like for hot-reload.
	/// For normal service resolution, you should request services in the constructor of your object.
	/// </summary>
	/// <param name="owner">The owner of the service provider</param>
	/// <returns>The service provider attached to the <paramref name="owner"/>, if any.</returns>
	public static IServiceProvider? FindProvider(object owner)
		=> _providers.TryGetValue(owner, out var provider) ? provider : null;
}
