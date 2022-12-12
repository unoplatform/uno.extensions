using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Uno.Extensions.Validation;

namespace TestHarness.Ext.Navigation.Validation;

[ReactiveBindable(false)]
public partial class ValidationOneViewModel : ObservableObject
{
	private readonly IValidator<SimpleEntity> _validator;
	public ValidationOneViewModel(IValidator<SimpleEntity> validator)
	{
		_validator= validator;
		_ = ValidateEntities();
	}

	private async Task ValidateEntities()
	{
		var entity = new SimpleEntity();
		var results = await _validator.ValidateAsync(entity);


	}


	public class SimpleEntity: IValidatableObject
	{
		public string Title { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
		{
			return string.IsNullOrWhiteSpace(Title) ?
				new ValidationResult[] {
					new ValidationResult( 
						errorMessage: "Title should not be null",
						memberNames: new[] {nameof(Title) }
						)
				} :
				default;
		}
	}
}

