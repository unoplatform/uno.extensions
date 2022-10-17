---
uid: Overview.Reactive.Rules
---
# Feeds code analyzers

## PS0001
**A property selector can only use property members.**

The _path_ of your `PropertySelector` tries to access something that is not a method.

Property selectors can only be of the form `e => e.A.B.C`, you cannot use method nor external value (i.e. cannot have any closure), and cannot be a method group.

## PS0002
**A property selector cannot have any closure.**

Your `PropertySelector` tries to use a captured value instead of the lambda argument.

Property selectors can only be of the form `e => e.A.B.C`, you cannot use method nor external value (i.e. cannot have any closure), and cannot be a method group.

## PS0003
**A property selector must be a lambda.**

Your `PropertySelector` is not a lambda expression.

Property selectors can only be of the form `e => e.A.B.C`, you cannot use method nor external value (i.e. cannot have any closure), and cannot be a method group.

## PS0004
**The `TEntity` of a `PropertySelector<TEntity, TValue>` must be a record**

The type of the _entity_ of your `PropertySelector` is not a record.

Convert the type to a record to use it with a `PropertySelector`.

## PS0005
**All types involved in a PropertySelector must be records.**

An intermediate type in the _path_ of your `PropertySelector` is not a record.

If you _path_ is `e => e.A.B.C`, make sure that `A` and `B` are records.

## PS0006
**All types involved in a PropertySelector must be constructable without parameter.**

An intermediate type in the _path_ of your `PropertySelector` does not have a constructor which is parameter-less or which accepts only nullable values.

If you _path_ is `e => e.A.B.C`, make sure that `A` and `B` have either a parameter-less constructor, either a constructor that accepts only nullable values.

For instance, considering a record `MyRecord`:
```csharp
public record MyRecord(string Value);
```

Valid constructors would be:
```csharp
public record MyRecord(string? Value);

/* OR */

public record MyRecord(string Value = "");

/* OR */

public record MyRecord(string Value)
{
	public MyRecord(): this("a default value") { }
}
```


## PS0101
**A method which accepts a PropertySelector must also have 2 parameters flagged with `[CallerFilePath]` and `[CallerLineNumber]`.**

You have a method that have a `PropertySelector` parameter but which is missing at least one argument flagged with `[CallerFilePath]` or `[CallerLineNumber]` attribute.

Add the missing argument.

Cf. ("Declare a PropertySelector parameter on a method")[Concept.md#Declare_a_PropertySelector_parameter_on_a_method] for more details.

## PS0102
**`[CallerFilePath]` and `[CallerLineNumber]` arguments used among a PropertySelector argument must be constant values.**

You are invoking a method that has a `PropertySelector` parameter but you are providing a non constant value for at least one argument flagged with `[CallerFilePath]` or `[CallerLineNumber]` attribute.

Remove the valeu to let the compiler full-fil it, or convert it to a constant value.

Cf. ("Declare a PropertySelector parameter on a method")[Concept.md#Declare_a_PropertySelector_parameter_on_a_method] for more details.



