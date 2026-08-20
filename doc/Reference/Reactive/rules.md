---
uid: Uno.Extensions.Reactive.Rules
---
# Feeds code analyzers

## Feed2001

**Unable to resolve the feed that is configured to be used as command parameter.**

You have a public method that should be converted into an `ICommand` with a parameter marked with the attribute `[FeedParameter("<property_name>")]`,
but no property named _property_name_ was not found in the class.

You have to ensure that the provided _property_name_ matches the name of a property in your class.

> [!TIP]
> Prefer to provide the _property_name_ using the `nameof` expression: `[FeedParameter(nameof(TheProperty))]`.
> This ensure better discoverability and increase maintainability as refactoring tools will automatically update the name.

## Feed2002

**The property configured to be used as command parameter is not a Feed of the right type.**

You have a public method that should be converted into an `ICommand` with a parameter of type `T` marked with the attribute `[FeedParameter("<property_name>")]`,
but the property _property_name_ is not of type `IFeed<T>` (nor `IState<T>`).

> [!NOTE]
> If your property is synchronous (i.e. not a `Feed` nor a `State`), you don't need to use the `[FeedParameter]` attribute.
> Remove the parameter from the method and get your value from the property directly.

## Feed3001

**Mock generation is enabled for a model, but its base model is not mock-enabled.**

Mock factories (`CreateMock` and the `{Vm}Mocks` bundle, cf. [Previewing and testing MVUX states](xref:Uno.Extensions.Mvux.Testing)) are generated only when the whole base-model chain is mock-enabled, because the mock constructors chain through the base view models. A model matched a `GenerateModelMocks` pattern, but one of its base models did not (or its assembly does not carry the attribute), so no mock factory is generated for it.

Extend the `GenerateModelMocks` patterns (in the base model's assembly if it lives in another assembly) to cover the base model, or exclude the derived model as well.

## Feed3002

**A GenerateModelMocks pattern does not match any generated bindable view model of the assembly.**

One of the patterns given to the assembly-level `GenerateModelMocks` attribute matched none of the models processed by the MVUX generator in this assembly — most likely a typo, a renamed model, or a pattern targeting a type that is not a generated model.

Note that patterns are regular expressions matched (unanchored) against the model's full name; anchor with `$` (e.g. `"MainModel$"`) to match exactly.
