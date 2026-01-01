using System.Diagnostics.CodeAnalysis;

namespace Uno.Extensions;

/// <summary>
/// Extensions for working with <see cref="IHostBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
	internal const string RequiresDynamicCodeMessage = "Binding strongly typed objects to configuration values may require generating dynamic code at runtime. [From Array.CreateInstance() and others.]";
	internal const string RequiresUnreferencedCodeMessage = "Cannot statically analyze the type of instance so its members may be trimmed. [From TypeDescriptor.GetConverter() and others.]";

	/// <summary>
	/// Registers storage services.
	/// </summary>
	/// <param name="hostBuilder">The host builder instance to register with</param>
	/// <param name="configure">Callback for configuring services</param>
	/// <returns>The updated host builder instance</returns>
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IHostBuilder UseStorage(
		this IHostBuilder hostBuilder,
		Action<IServiceCollection> configure)
			=> hostBuilder.UseStorage((context, builder) => configure.Invoke(builder));

	/// <summary>
	/// Registers storage services
	/// </summary>
	/// <param name="hostBuilder">The host builder instance to register with</param>
	/// <param name="configure">Callback for configuring services</param>
	/// <returns></returns>
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IHostBuilder UseStorage(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, IServiceCollection>? configure = default)
	{
		return hostBuilder
			.UseSerialization()
			.UseConfiguration(
				configure: configBuilder =>
				{
					if (configBuilder.IsRegistered(nameof(KeyValueStorageConfiguration)))
					{
						return configBuilder;
					}

					return configBuilder
							.Section<KeyValueStorageConfiguration>(nameof(KeyValueStorageConfiguration));
				})
			.ConfigureServices((ctx, services) =>
			{
				if (!ctx.IsRegistered(nameof(UseStorage)))
				{
					_ = services
						.AddFileStorage()
						.AddKeyedStorage();
				}
				configure?.Invoke(ctx, services);
			});
	}
}
