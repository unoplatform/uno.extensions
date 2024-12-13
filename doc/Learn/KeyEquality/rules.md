---
uid: Uno.Extensions.KeyEquality.Rules
---
# Feeds code analyzers

## KE0001

**A record eligible for IKeyEquatable generation must be partial.**

You have a record that has a property that can be used to generate `IKeyEquatable` implementation for that record,
but it was not declared as `partial`.

Add the `partial` modifier on your record, or turn off the `IKeyEquatable` implementation generated using `[ImplicitKeyEquality(IsEnabled = false)]` on the record itself or the whole assembly (not recommended).

## KE0002

**A record that implements GetKeyHashCode should also implement KeyEquals.**

You have a method named `GetKeyHashCode` in your record, but no `KeyEquals` exist.

If you want to define a custom way to compute _key equality_, you have to implement both the `GetKeyHashCode` and `KeyEquals`.

## KE0003

**A record that implements KeyEquals should also implement GetKeyHashCode.**

You have a method named `KeyEquals` in your record, but there is no `GetKeyHashCode`.

If you want to define a custom way to compute _key equality_, you have to implement both the `GetKeyHashCode` and `KeyEquals`.

## KE0004

**A record flagged with `[ImplicitKeyEquality]` attribute must have an eligible key property**

You have a record flagged with the `[ImplicitKeyEquality]` attribute, but no property matches any of the defined implicit keys.

You should remove the `[ImplicitKeyEquality]` attribute or add a valid property name.

## KE0005

**A record should have only one matching key property for implicit IKeyEquatable generation.**

You have a record eligible for implicit key equality generation, but it has more than one matching implicit key. The generated implementation of `IKeyEquatable` will use only the first key.

You should either explicitly flag all needed key properties with the `[Key]` attribute or remove/rename properties that should not be used as keys.

> [!NOTE]
> By default, the key equality generation will search for `Id` or `Key` properties.
> You can customize it using the `ImplicitKeyEquality` attribute.
>, For instance, setting `[assembly:ImplicitKeyEquality("Id", "MyCustomKey")]` on your assembly will no longer search for `Key` properties,
> but will instead search for `MyCustomKey` properties.
