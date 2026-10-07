---
uid: Uno.Extensions.Mvux.Advanced.Validation
---

# Validation

> **UnoFeatures:** `MVUX` (add to `<UnoFeatures>` in your `.csproj`)

MVUX lets a state carry validation results alongside its value. The generated view model then exposes them through the standard [`INotifyDataErrorInfo`](https://learn.microsoft.com/dotnet/api/system.componentmodel.inotifydataerrorinfo) interface, so you don't have to write a `FirstNameError` feed for every field.

- Validation **never blocks a value**. An invalid value is still set on the state, and the validation results only annotate it.
- Validation results are standard [`ValidationResult`](https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations.validationresult) instances. MVUX does not depend on `Uno.Extensions.Validation`, but its `IValidator` plugs in directly (see [below](#using-the-ivalidator-service)).
- Validation is opt-in. When you don't use it, it costs nothing.

## Validating a state

Use `Validate` on a state to validate its value each time it changes:

```csharp
public partial record PersonModel
{
    public IState<string> Name => State.Value(this, () => string.Empty)
        .Validate(async (name, ct) => string.IsNullOrWhiteSpace(name)
            ? new[] { new ValidationResult("Name is required", new[] { nameof(Name) }) }
            : Enumerable.Empty<ValidationResult>());
}
```

The validator:

- runs on a background thread for the initial value and then after every data change, including changes made from the UI through two-way bindings;
- is cancelled (through its `CancellationToken`) when the value changes again before it completes. Results produced for a value that is no longer the current value of the state are discarded;
- is not invoked when the state has no value. In that case the validation results are cleared;
- does not put the state in error if it throws. The exception is logged and the previous results are kept.

`Validate` returns the same state instance and can safely be invoked each time the property getter is evaluated: the last validator wins and validators are never stacked.

### Single-rule validators

For a single rule, overloads let you return only the error message, or a predicate and its message, instead of building the list of `ValidationResult`:

```csharp
public partial record PersonModel
{
    // Returns the error message, or null when the value is valid.
    public IState<string> Name => State.Value(this, () => string.Empty)
        .Validate(async (name, ct) => string.IsNullOrWhiteSpace(name) ? "Name is required" : null);

    // Returns true when the value is valid.
    public IState<int> Age => State.Value(this, () => 0)
        .Validate(async (age, ct) => age is >= 0 and <= 150, "Age must be between 0 and 150");
}
```

The error is reported for the state itself (empty `MemberNames`, cf. [Consuming errors in the view](#consuming-errors-in-the-view)). Use the overload returning a list of `ValidationResult` to report several errors, or errors targeting members of a record.

> [!NOTE]
> A validator lambda which only throws (no `return`) matches several overloads. Give it an explicit return type, for example `ValueTask<IEnumerable<ValidationResult>> (string name, CancellationToken ct) => throw ...`.

### Using the `IValidator` service

The `IValidator` service of [Uno.Extensions.Validation](xref:Uno.Extensions.Validation.Overview) matches the shape of the validator delegate, so it can be used as is:

```csharp
public partial record PersonModel(IValidator Validator)
{
    public IState<Person> Person => State.Value(this, () => new Person())
        .Validate((person, ct) => Validator.ValidateAsync(person, null, ct));
}
```

### Localizing messages

When the app uses [localization](xref:Uno.Extensions.Localization.Overview), inject the `IStringLocalizer` in the model and pass it as the last argument of `Validate`, with any overload: the error messages are then resource keys, resolved through the localizer.

```csharp
public partial record PersonModel(IStringLocalizer Localizer, IValidator Validator)
{
    // Returns the resource key of the error message, or null when the value is valid.
    public IState<string> Name => State.Value(this, () => string.Empty)
        .Validate(async (name, ct) => string.IsNullOrWhiteSpace(name) ? "Validation_NameRequired" : null, Localizer);

    // Returns true when the value is valid.
    public IState<int> Age => State.Value(this, () => 0)
        .Validate(async (age, ct) => age is >= 0 and <= 150, "Validation_AgeRange", Localizer);

    // The ErrorMessage of each result is a resource key, e.g. [Required(ErrorMessage = "Validation_EmailRequired")].
    public IState<Person> Person => State.Value(this, () => new Person())
        .Validate((person, ct) => Validator.ValidateAsync(person, null, ct), Localizer);
}
```

- This is the only place where validation messages are localized: `Uno.Extensions.Validation` reports the `ErrorMessage` of DataAnnotations attributes and the `WithMessage` of FluentValidation as written, so use resource keys there (see [validation messages](xref:Uno.Extensions.Validation.Overview#validation-messages)).
- Messages are resolved each time the validator produces results, using the current culture. As changing the culture [requires an app restart](xref:Uno.Extensions.Localization.Overview#ui-culture), messages already produced are not updated.
- A key which is not found in the resources keeps the message as produced by the validator, so non-localized messages can be mixed with keys.
- The `MemberNames` of the results are kept.
- Resolved messages are not formatted: a `{0}` in a resource is displayed as is. For messages with arguments, don't pass the localizer to `Validate`, and use it in the validator instead:

  ```csharp
  .Validate(async (person, ct) => person.Age < 18
      ? [new ValidationResult(Localizer["Validation_MinAge", 18], [nameof(Person.Age)])]
      : [])
  ```

### Setting validation results manually

Validation results are a metadata axis of the messages of the state, like the error or progress. You can set them yourself, for instance to validate the whole form when the user clicks Save:

```csharp
public async ValueTask Save(CancellationToken ct)
{
    var person = await Person;
    var results = (await Validator.ValidateAsync(person!, null, ct)).ToList();

    await Person.UpdateMessageAsync(msg => msg.Validation(results), ct);
    if (results.Count is 0)
    {
        // Save the person
    }
}
```

Passing `null` or an empty list clears the results. Setting results identical to the current ones (same `ErrorMessage` and `MemberNames`) does not raise any change.

> [!NOTE]
> Validation results are local to the state that has them: they are **not** forwarded to feeds derived from it, such as `Select`, `Combine` or a `Feed.Async` that awaits the state. A derived `FullName` feed never shows the errors of the `Person` state.

## Consuming errors in the view

The generated view model, and the generated bindable of each record, implement `INotifyDataErrorInfo`. Results are routed to the object that owns the bound property according to their `MemberNames`:

| Result `MemberNames` (on state `Person`) | Where the error is exposed |
| --- | --- |
| _empty_ or `["Person"]` | `GetErrors("Person")` on the view model, and entity-level errors (`GetErrors(null)`) of the `Person` bindable |
| `["FirstName"]` | `GetErrors("FirstName")` on the `Person` bindable, i.e. the source object of `{Binding Person.FirstName}` |
| `["Address.Street"]` | `GetErrors("Street")` on the `Person.Address` bindable |
| `["Address"]` | `GetErrors("Address")` on the `Person` bindable, and entity-level errors of the `Person.Address` bindable |

For a state of a type that has no generated bindable (such as `IState<string> Name`), every result is exposed as `GetErrors("Name")` on the view model.

`HasErrors` is `true` when the object or any of its nested bindables has errors. It is a public property, so you can bind to it, for example to show a message or to disable a button:

```xml
<TextBox Text="{Binding Person.FirstName, Mode=TwoWay}" />
<TextBlock Text="Please fix the highlighted fields"
           Visibility="{Binding HasErrors}" />
```

> [!IMPORTANT]
> WinUI controls don't render `INotifyDataErrorInfo` errors by themselves. Display errors from your own templates or bindings, for example by binding to `HasErrors` or by reading `GetErrors` from code.

### Name collisions

If a model or a record declares its own `HasErrors` member, the generated member of the same name hides the `HasErrors` property of the bindable, and the generator reports the informational diagnostic [`FEED1001`](xref:Uno.Extensions.Reactive.Rules). Validation still works through the `INotifyDataErrorInfo` interface, but you can't bind to `HasErrors` by name on that object.

If you declare a `HasErrors` member yourself in a hand-written `partial` of a generated view model, the compiler reports `CS0108` (member hides inherited member). Add the `new` modifier to your member, or rename it.

## Current limitations

- Only states (`IState<T>`) are validated. Read-only feeds and item-level errors of list states (`IListState<T>`) are not supported yet.
- Validation runs on every data change, including the initial value. There is no built-in "touched" tracking yet: use the manual approach above to validate only when the user submits the form.
- Commands are not disabled automatically when there are errors. Bind to `HasErrors` or check the validation results in the command.
