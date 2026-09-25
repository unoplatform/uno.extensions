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

## Mock0001

**No mock is generated for a model whose view-model has no public constructor.**

`Uno.HotTesting.Reactive` builds the real view-model with every constructor parameter null-injected
(`{Vm}Mock.Create`). The generated view-model mirrors the model's constructors, so a model whose constructors
are all `internal` or `private` leaves nothing for `Create` to call: the generator reports this warning and
emits no `{Model}Mock` / `{Vm}Mock` for that model.

Make one of the model's constructors public to get the mock back, or suppress the warning for a model that is
not meant to be mocked.

## Mock0002

**No mock is generated for a model none of whose feeds is fed by a constructor parameter.**

A `{Model}Mock` replaces the model's _inputs_ — the feeds reading a service the model takes as a constructor
parameter — and the feeds derived from them. A model whose feeds are all independent has nothing a mock could
drive, so the generator emits none and says so rather than leaving you to wonder why nothing appeared.

If you expected a mock, check that the feed really reaches a constructor parameter: a service obtained through
a helper method or a locator is not visible to the analysis. Declaring
`[FeedDependency("Member", OnParameter = "service")]` on the model states the dependency explicitly and takes
precedence over what is inferred.

This one is reported at information level, so that a model whose feeds are legitimately all independent cannot
fail a build treating warnings as errors. `dotnet build` hides it at default verbosity: read it in the IDE's
error list, build with `-v normal`, or raise it in `.editorconfig` with
`dotnet_diagnostic.MOCK0002.severity = warning`.

## Mock0003

**An implicit model pattern is not a valid regular expression.**

`[assembly: ImplicitBindables("…")]` decides which types are treated as models. A pattern that cannot compile
matches nothing, so the types you expected to be models silently are not — and neither view-models nor mocks
are generated for them. The warning names the offending pattern; fix or remove it.
