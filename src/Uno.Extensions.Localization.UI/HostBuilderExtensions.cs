using System.Diagnostics.CodeAnalysis;
using Uno.Extensions.Hosting;

namespace Uno.Extensions.Localization;

/// <summary>
/// Extensions for configuring localization.
/// </summary>
public static class HostBuilderExtensions
{
	internal const string RequiresDynamicCodeMessage = "Binding strongly typed objects to configuration values may require generating dynamic code at runtime. [From Array.CreateInstance() and others.]";
	internal const string RequiresUnreferencedCodeMessage = "Cannot statically analyze the type of instance so its members may be trimmed. [From TypeDescriptor.GetConverter() and others.]";

	/// <summary>
	/// Configures the localization service.
	/// </summary>
	/// <param name="hostBuilder">
	/// The host builder to configure.
	/// </param>
	/// <param name="configure">
	/// An action that configures the localization service.
	/// </param>
	/// <returns>
	/// The host builder for chaining.
	/// </returns>
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IHostBuilder UseLocalization(
		this IHostBuilder hostBuilder,
		Action<IServiceCollection> configure)
	{
		return hostBuilder.UseLocalization((context, builder) => configure.Invoke(builder));
	}

	/// <summary>
	/// Configures the localization service.
	/// </summary>
	/// <param name="hostBuilder">
	/// The host builder to configure.
	/// </param>
	/// <param name="configure">
	/// An action that configures the localization service. Optional.
	/// </param>
	/// <returns>
	/// The host builder for chaining.
	/// </returns>
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IHostBuilder UseLocalization(
		this IHostBuilder hostBuilder,
		Action<HostBuilderContext, IServiceCollection>? configure = default)
	{
		if (hostBuilder.IsRegistered(nameof(UseLocalization)))
		{
			return hostBuilder;
		}

		return hostBuilder
			.UseConfiguration(
				configure: configBuilder =>
					configBuilder
						.Section<LocalizationConfiguration>(nameof(LocalizationConfiguration))
						.Section<LocalizationSettings>(nameof(LocalizationSettings))
					)

			.ConfigureServices((ctx, services) =>
		{
			_ = services
			.AddSingleton<LocalizationService>()
			.AddSingleton<IServiceInitialize>(sp => sp.GetRequiredService<LocalizationService>())
			.AddSingleton<ILocalizationService>(sp => sp.GetRequiredService<LocalizationService>())
			.AddSingleton<IStringLocalizer, ResourceLoaderStringLocalizer>();
		});
	}
}
