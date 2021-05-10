namespace ApplicationTemplate
{
	/// <summary>
	/// Overrides the default validation messages for FluentValidation.
	/// </summary>
	public class FluentValidationLanguageManager : FluentValidation.Resources.LanguageManager
	{
		public FluentValidationLanguageManager()
		{
			// Remove {PropertyName} from the error message, since the property name itself is not localized.
			AddTranslation("en", "NotNullValidator", "This property is required.");
			AddTranslation("fr", "NotNullValidator", "Cette propriété est requise.");

			AddTranslation("en", "NotEmptyValidator", "This property is required.");
			AddTranslation("fr", "NotEmptyValidator", "Cette propriété est requise.");
		}
	}
}
