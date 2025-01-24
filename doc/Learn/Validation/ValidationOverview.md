---
uid: Uno.Extensions.Validation.Overview
---

# Validation

Uno.Extensions.Validation provides an `IValidator` service which enforces that properties of an object follow a set of characteristics or behaviors represented by [attributes](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations) declaratively applied to them. This is useful for ensuring that data entered by users is valid before it is saved to a database or sent to a web service.

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

[!include[existing-app](../includes/existing-app.md)]

By default, enabling validation will register the default `Validator` type with the DI container. This class implements `IValidator` and provides functionality to execute validation rules on validatable entities.

## Using the validation service

The `IValidator` service is injected into the constructor of view models which specifies the `IValidator` service as a dependency. View models that need to validate data can then use the `ValidateAsync` method to execute validation rules on an object. The `ValidateAsync` method returns a `ValueTask<T>` that completes when validation is complete. The `ValueTask` result is an `IEnumerable<ValidationResult>` which contains elements that indicate whether the object is valid or not.

The following example shows how to use the service to validate a `Person` object:

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

The service checks whether the `Person` object is valid and handles calling the correct validator registered for the object type. It returns a `ValueTask<T>` that completes when validation is complete. The `ValueTask` result is an `IEnumerable<ValidationResult>` which contains elements that indicate whether the object is valid or not. Uno Extensions enables fine-grained control over how validation is performed by allowing systematic use of a number of patterns from the .NET ecosystem. This is done by offering customization of both the validatable entity and within a validator service itself.

## Validatable entities

Instances of a class that are passed into the `ValidateAsync` method of the default validator have three primary implementation options to allow for validation:

### [**INotifyDataErrorInfo**](#tab/notify-data-error-info)

A common choice for entities that need to implement validation is the `INotifyDataErrorInfo` interface. Internally, the `GetErrors` method exposed by these objects is called by the validator. The .NET Community Toolkit provides an `ObservableValidator` class that implements this interface and provides the base functionality to execute validation rules on properties that have been decorated with validation attributes. `ObservableValidator` is a good choice for objects that need to be observable because it also implements `ObservableObject`.

#### Code example

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

It is also possible to define custom validation attributes by deriving from the `ValidationAttribute` class. More information on custom validation can be found [here](https://nicksnettravels.builttoroam.com/custom-validation/).

For more information, see the reference documentation about [INotifyDataErrorInfo](https://learn.microsoft.com/dotnet/api/system.componentmodel.inotifydataerrorinfo) and [ObservableValidator](https://docs.microsoft.com/windows/communitytoolkit/mvvm/observablevalidator).

### [**IValidatableObject**](#tab/validatable-object)

Another option is the `IValidatableObject` interface. Like `INotifyDataErrorInfo`, it is implemented by a class to ensure properties can be validated based on the attributes they are decorated with. Validator support for this interface is provided internally with the `TryValidateObject` method used by the default validator to execute validation rules on the entity in question. This method returns an `IEnumerable<ValidationResult>` which contains elements that indicate whether the object is valid or not.

Unlike other options, `IValidatableObject` based types can define more advanced validation behavior in the `Validate` method implementation. Here, it is typical to complement the behavior of attributes by invoking `TryValidateProperty` on multiple members, or to do any other kind of inter-property validation. The default validator is responsible for invoking this `Validate` method.

#### Code example

```csharp
public class Person : IValidatableObject
{
    [Required]
    public string FirstName { get; set; }

    [Required]
    public string LastName { get; set; }

    [Range(0, 100)]
    public int Age { get; set; }

    [Phone]
    public string PhoneNumber { get; set; }

    public bool IsDeceased { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        if (IsDeceased && !string.IsNullOrEmpty(PhoneNumber))
        {
            results.Add(new ValidationResult("A deceased person cannot have a phone number", new[] { nameof(PhoneNumber) }));
        }

        return results;
    }
}
```

For more information, see the reference documentation about [`IValidatableObject`](https://docs.microsoft.com/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject), [`CustomValidationAttribute`](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations.customvalidationattribute).

### [**Fluent**](#tab/fluent-validation)

[FluentValidation](https://www.nuget.org/packages/FluentValidation/) is a popular choice for entities needing to implement validation. It is a library that provides a fluent API for defining validation rules. This choice allows for chained validator calls and minimal modifications to the model definition. It is possible to use a [Fluent validator](https://docs.fluentvalidation.net/en/latest/start.html) by registering the implementation like below:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs e)
{
    // Register the Fluent Validator type for 
    // use on a Person entity
    var appBuilder = this.CreateBuilder(args)
        .Configure(host => {
            host
            .UseValidation(configure: (validationBuilder, hostBuilder) => 
                validationBuilder.Validator<Person, PersonValidator>()
            );
        });
...
```

---

## See also

- [Information on FluentValidation](https://fluentvalidation.net/)
- [INotifyDataErrorInfo](https://learn.microsoft.com/dotnet/api/system.componentmodel.inotifydataerrorinfo)
- [IValidatableObject](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.ivalidatableobject)
- [Common attributes](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations)
- [Custom validation attributes](https://nicksnettravels.builttoroam.com/custom-validation/)
