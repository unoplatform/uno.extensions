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
