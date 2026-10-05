namespace Uno.Extensions.Validation;

/// <summary>
/// Options of the localized DataAnnotations validation, enabled using <see cref="DataAnnotationsValidationBuilderExtensions.UseLocalizedDataAnnotations"/>.
/// </summary>
public class DataAnnotationsLocalizationOptions
{
	/// <summary>
	/// Gets or sets the delegate which provides the <see cref="IStringLocalizer"/> used to localize the messages of a validated type.
	/// </summary>
	/// <remarks>
	/// The default resolves the non-generic <see cref="IStringLocalizer"/> from the services (registered by <c>UseLocalization</c>).
	/// When it returns null, the type is validated exactly as without localization.
	/// </remarks>
	public Func<Type, IServiceProvider, IStringLocalizer?> LocalizerProvider { get; set; } = static (_, services) => services.GetService<IStringLocalizer>();

	/// <summary>
	/// Gets or sets the composite format of the resource key used for attributes which do not set <see cref="ValidationAttribute.ErrorMessage"/>,
	/// where <c>{0}</c> is the attribute type name without the <c>Attribute</c> suffix (e.g. <c>"Validation_{0}"</c> gives <c>Validation_Required</c>).
	/// </summary>
	/// <remarks>When null (default), those attributes keep their default message.</remarks>
	public string? DefaultMessageKeyFormat { get; set; }
}
