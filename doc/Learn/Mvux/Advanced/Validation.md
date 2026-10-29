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

- runs on the thread pool for the initial value and then after every data change, including changes made from the UI through two-way bindings. On WebAssembly, where there is a single thread, it runs on the UI thread, so avoid long synchronous work in it;
- is cancelled (through its `CancellationToken`) when the value changes again before it completes. Results produced for a value that is no longer the current value of the state are discarded. A validator that ignores its token keeps running until it completes, so a slow validator (e.g. calling a server) should honor the token and apply its own timeout;
- is not invoked when the state has no value (`None`). In that case the validation results are cleared. A `null` value is still a value, so the validator is invoked with `null`;
- does not put the state in error if it throws. The exception is logged and the value has no results: the results of a previous value are not kept for it, so `HasErrors` being `false` does not mean that the value has been validated.

The results of a value are kept until the next validation: they are not lost when the source of the state produces the same value again (e.g. a refresh of a `State.Async`), and they are not reported for another value while it is being validated.

`Validate` returns the same state instance and can safely be invoked each time the property getter is evaluated: the last validator wins and validators are never stacked.

### Using the `IValidator` service

The `IValidator` service of [Uno.Extensions.Validation](xref:Uno.Extensions.Validation.Overview) plugs in with a one-line lambda:

```csharp
public partial record PersonModel(IValidator Validator)
{
    public IState<Person> Person => State.Value(this, () => new Person())
        .Validate((person, ct) => Validator.ValidateAsync(person, null, ct));
}
```

### Setting validation results manually

Validation results are a metadata axis of the messages of the state, like the error or progress. You can set them yourself with `SetValidationAsync`, for instance to validate the whole form when the user clicks Save:

```csharp
public async ValueTask Save(CancellationToken ct)
{
    var person = await Person;
    var results = (await Validator.ValidateAsync(person!, null, ct)).ToList();

    await Person.SetValidationAsync(results, ct);
    if (results.Count is 0)
    {
        // Save the person
    }
}
```

Passing `null` or an empty list clears the results. Setting results identical to the current ones (same `ErrorMessage` and `MemberNames`) does not raise any change. The results are kept until they are replaced, by another call or, if a validator has been set with `Validate`, by its results on the next change of the value. Like the results of a validator, they are not dropped when the source of the state produces a new value.

You can also set the validation along with other axes of the message using `UpdateMessageAsync(msg => msg.Validation(results), ct)`. Results set this way follow the rules of the other updates of the state: they are dropped when the source of the state produces a new value. `SetValidationAsync` falls back to it for a state that has not been created by the `State` factories.

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
| `["Nmae"]` (not a property of `Person`) | entity-level errors (`GetErrors(null)`) of the `Person` bindable, so they are not lost |

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

If you declare a `HasErrors` member yourself in a hand-written `partial` of a generated view model, the generator doesn't add its own `HasErrors` to that view model and reports `FEED1001` too: `{Binding HasErrors}` resolves to your member, while the `INotifyDataErrorInfo` implementation keeps working.

## Current limitations

- Only states (`IState<T>`) are validated. Read-only feeds and item-level errors of list states (`IListState<T>`) are not supported yet.
- Validation runs on every data change, including the initial value. There is no built-in "touched" tracking yet: use the manual approach above to validate only when the user submits the form.
- Commands are not disabled automatically when there are errors. Bind to `HasErrors` or check the validation results in the command.
