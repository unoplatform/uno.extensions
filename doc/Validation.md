# Validation
_[TBD - Review and update this guidance]_

We use [FluentValidation](https://www.nuget.org/packages/FluentValidation/) to execute validation rules.

## Form validation

You can use the `AbstractValidator` to validate a form view model.

- `FluentValidation` is compatible with `IStringLocalizer` if you want to use specific resources. `FluentValidation` comes with default resources that can be ovewritten. This is configured in the [FluentValidationLanguageManager.cs](../src/app/ApplicationTemplate.Shared/Configuration/FluentValidationLanguageManager.cs) file.

- It is also compatible with `GenericHost` to support easy dependency injection of the validators. You can easily resolve any validators as they are registered using `AddValidatorsFromAssemblyContaining` in the [ViewModelConfiguration.cs](../src/app/ApplicationTemplate.Shared/Configuration/ViewModelConfiguration.cs) file.

```csharp
public class LoginFormViewModel : ViewModel
{
    public LoginFormViewModel()
    {
        // This is not required but will add auto-validation when the properties change.
        this.AddValidation(this.GetProperty(x => x.Email));
        this.AddValidation(this.GetProperty(x => x.Password));
    }

    public string Email
    {
        get => this.Get<string>();
        set => this.Set(value);
    }

    public string Password
    {
        get => this.Get<string>();
        set => this.Set(value);
    }
}

// This validator is registered automatically into the IoC
public class LoginFormValidator : AbstractValidator<LoginFormViewModel>
{
    public LoginFormValidator(IStringLocalizer localizer)
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage(_ => localizer["ValidationNotEmpty_Email"]).EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

// You can then validate the form by simply doing this.
// The validator will be resolved from the IoC and execute the validation on the view model.
var validationResult = await Form.Validate(ct);
```

## Reference

- [For more information on FluentValidation](https://fluentvalidation.net/).