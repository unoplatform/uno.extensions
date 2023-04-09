---
uid: Overview.Validation
---

# Validation

Uno.Extensions.Validation provides an `IValidator` service to enforce that properties on an object follow a set of characteristics or behaviors represented by [attributes](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations) declaratively applied to them. This is useful for ensuring that data entered by users is valid before it is saved to a database or sent to a web service.

## Service registration

An extension method for `IHostBuilder` is provided to register the `IValidator` service with the DI container.

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e){
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseValidation();
        });
...
```

## Validation service

It can then be injected into any class that needs to validate data. The `ValidateAsync` method is used to execute validation rules on an object. The method returns a `ValueTask` that completes when validation is complete. The `ValueTask` result is a `IEnumerable<ValidationResult>` which contains elements which indicate whether the object is valid or not.

By default, enabling validation will register the default `Validator` type with the DI container. This class implements `IValidator` and provides functionality to execute validation rules on validation-capable classes. However, Uno Extensions enables fine-grained control over how validation is performed by allowing systematic use of a number of patterns from the .NET ecosystem. This is done by offering customization of both the validation-capable class and validator service itself.

## Validation-capable classes

Instances passed into the `ValidateAsync` method of the default validator have three primary implementation options for being validation-capable:

# [**INotifyDataErrorInfo**](#tab/notify-data-error-info)
A common choice for classes that need to implement validation is the `INotifyDataErrorInfo` interface. Internally, the `GetErrors` method exposed by these objects is called by the validator. The .NET Community Toolkit provides an `ObservableValidator` class that implements this interface and provides the base functionality to execute validation rules on properties that have been decorated with validation attributes. `ObservableValidator` is a good choice for objects that need to be observable because it also implements `ObservableObject`. 


### Code example

An example using validation attributes is shown below:

```csharp
public class Person : ObservableValidator
{
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Range(0, 100)]
    public int Age { get; set; }
}
```

The `ObservableValidator` class implements `INotifyDataErrorInfo` and provides functionality to execute validation rules on properties that have been decorated with validation attributes. It is also possible to define custom validation attributes by deriving from the `ValidationAttribute` class. More information on custom validation can be found in [this](https://nicksnettravels.builttoroam.com/custom-validation/) blog post.

For more information, see [ObservableValidator](https://docs.microsoft.com/windows/communitytoolkit/mvvm/observablevalidator).
# [**IValidatableObject**](#tab/validatable-object)
Another option with a more recent history is the `IValidatableObject` interface. This interface is supported by the `TryValidateObject` method used internally by the default validator to execute validation rules on the entity in question. The `TryValidateObject` method returns a `IEnumerable<ValidationResult>` which contains elements which indicate whether the object is valid or not. 

Unlike other implementation options, the validation behavior for `IValidatableObject` based types is defined by the `Validate` method which itself usually invokes the `TryValidateProperty` method when it's called by the default validator.

### Code example

```csharp
public class Person : IValidatableObject
{
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Range(0, 100)]
    public int Age { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (!TryValidateProperty(FirstName, new ValidationContext(this, null, null) { MemberName = nameof(FirstName) }, results))
        {
            results.Add(new ValidationResult("First name is invalid."));
        }
        
        if (!TryValidateProperty(LastName, new ValidationContext(this, null, null) { MemberName = nameof(LastName) }, results))
        {
            results.Add(new ValidationResult("Last name is invalid."));
        }

        return results;
    }
}
```

For more information, see [IValidatableObject](https://docs.microsoft.com/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject).
# [**Fluent**](#tab/fluent-validation)
The last option is to use [FluentValidation](https://www.nuget.org/packages/FluentValidation/) to execute validation rules. This choice allows for chained validator calls and minimal modifications to the model definition. 

Check out the [section](https://docs.fluentvalidation.net/en/latest/di.html#) on how to configure validators.

---

## Validating objects

As described above, the service provides a `ValidateAsync` method that can be used to validate an object. The following example shows how to use the service to validate a `Person` object:

```csharp
public class PersonViewModel
{
    private readonly IValidator _validator;

    public PersonViewModel(IValidator validator)
    {
        _validator = validator;
        Person = new Person()
        {
            FirstName = "John",
            // Leaving this out will cause validation to fail
            // LastName = "Doe",
            Age = 50
        };
    }

    public Person Person { get; private set; }

    public async Task ValidatePersonAsync()
    {
        var results = await _validator.ValidateAsync(Person);
        if (results.Any())
        {
            // Handle validation errors
        }
    }
}
```

The service checks whether the `Person` object is valid and handles calling the correct validator registered for the object type. It returns a `ValueTask` that completes when validation is complete. The `ValueTask` result is a `IEnumerable<ValidationResult>` which contains elements which indicate whether the object is valid or not.

## See also

- [Information on FluentValidation](https://fluentvalidation.net/)
- [INotifyDataErrorInfo](https://learn.microsoft.com/dotnet/api/system.componentmodel.inotifydataerrorinfo)
- [IValidatableObject](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject)
- [Common attributes](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations)
- [Custom validation attributes](https://nicksnettravels.builttoroam.com/custom-validation/)