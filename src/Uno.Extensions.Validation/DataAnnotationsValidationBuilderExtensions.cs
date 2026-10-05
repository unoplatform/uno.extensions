namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IValidationBuilder"/> to configure the DataAnnotations validation.
/// </summary>
public static class DataAnnotationsValidationBuilderExtensions
{
	/// <summary>
	/// Localizes the messages of the DataAnnotations validation using an <see cref="IStringLocalizer"/>.
	/// </summary>
	/// <param name="builder">The validation builder.</param>
	/// <param name="configure">Callback to configure the <see cref="DataAnnotationsLocalizationOptions"/>.</param>
	/// <returns>The validation builder, so it can be used to chain other configuration.</returns>
	/// <remarks>
	/// The <see cref="ValidationAttribute.ErrorMessage"/> of attributes and the <see cref="DisplayAttribute.Name"/> of properties are used as resource keys.
	/// A key not found in the localizer keeps the non-localized message (or name).
	/// Attributes using <see cref="ValidationAttribute.ErrorMessageResourceType"/> are not affected.
	/// </remarks>
	public static IValidationBuilder UseLocalizedDataAnnotations(
		this IValidationBuilder builder,
		Action<DataAnnotationsLocalizationOptions>? configure = null)
	{
		return builder
			.ConfigureServices((_, services) =>
			{
				services.TryAddSingleton<LocalizedDataAnnotationsValidator>();
				var options = services.AddOptions<DataAnnotationsLocalizationOptions>();
				if (configure is not null)
				{
					options.Configure(configure);
				}
			})
			.AsValidationBuilder();
	}
}
