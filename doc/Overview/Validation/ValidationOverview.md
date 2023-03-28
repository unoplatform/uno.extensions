---
uid: Overview.Validation
---

# Validation

Uno.Extensions.Validation provides an `IValidator` service to validate that properties on an object follow a set of characteristics or behaviors defined by [attributes](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations) applied to them. This is useful for ensuring that data entered by users is valid before it is saved to a database or sent to a web service.

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

## Validation-capable classes

Instances passed into the `ValidateAsync` method should implemement `INotifyDataErrorInfo`. It follows that there are two primary options for making a class validation-capable:

- Inherit from `ObservableValidator` and use attributes on properties to define validation rules such as `[Required]`
- Use [FluentValidation](https://www.nuget.org/packages/FluentValidation/) to execute validation rules. Check out the [section](https://docs.fluentvalidation.net/en/latest/di.html#) on how to configure validators

## Validation attributes

An example of using validation attributes is shown below:

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

The `ObservableValidator` class implements `INotifyDataErrorInfo` and provides functionality to execute validation rules on properties that have been decorated with validation attributes.

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

## See also

- [Information on FluentValidation](https://fluentvalidation.net/)
- [INotifyDataErrorInfo](https://learn.microsoft.com/dotnet/api/system.componentmodel.inotifydataerrorinfo)
- [Common attributes](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations)