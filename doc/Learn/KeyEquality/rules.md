---
uid: Uno.Extensions.Reactive.Rules
---
# Feeds code analyzers

## KE0001
**A record eligible to IKeyEquatable generation must be partial.**

You have a record that has a property that can be used to generate `IKeyEquatable` implemenation for that record,
but it was not delacred as `partial`.

Add the `partial` modifier on your record, or disable the `IKeyEquatable` implemenation generated using `[ImplicitKeyEquality(IsEnabled = false)]`
 on the record itself or on the whole assembly (not recommended).

## KE0002
**A record that implements GetKeyHashCode should also implement KeyEquals.**

You have method named `GetKeyHashCode` in your record, but there is no `KeyEquals`.

If you want to define a custom way to compute _key equality_, you have to implement both the `GetKeyHashCode` and `KeyEquals`.

## KE0003
**A record that implements KeyEquals should also implement GetKeyHashCode.**

You have method named `KeyEquals` in your record, but there is no `GetKeyHashCode`.

If you want to define a custom way to compute _key equality_, you have to implement both the `GetKeyHashCode` and `KeyEquals`.

## KE0004
**A record flagged with `[ImplicitKeyEquality]` attribute must have an eligible key property**

You have a record that is flagged with `[ImplicitKeyEquality]` attribute, but there is no property that match any of the defined implicit keys.

You should either remove the `[ImplicitKeyEquality]` attribute, either add a valid property name.

## KE0005
**A record should have only one matching key property for implicit IKeyEquatable generation.**

You have a record that is eligible for implicit key equality generation, but it has more than one matching implicit key.
The generated implementation of `IKeyEquatable` will use only the first key.

You should either explicitly flag all needed key properties with the `[Key]` attribute, 
either remove/rename properties that should not be used as key.

> [!NOTE]
> By default the key equlity generation will serach for properties named `Id` or `Key`.
> You can customize it using the `ImplicitKeyEquality` attribute. 
> For instance setting `[assembly:ImplicitKeyEquality("Id", "MyCustomKey")]` on your assembly will no longer search for `Key` properties, 
> but will instead serach for `MyCustomKey` properties.
