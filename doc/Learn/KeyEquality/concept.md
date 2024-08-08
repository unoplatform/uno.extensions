---
uid: Uno.Extensions.KeyEquality.Concept
---
# Concept

When working with immutable objects (usually `record`), each modification on an entity means that a new instance is created.
While this offers lots of [great advantages](https://en.wikipedia.org/wiki/Immutable_object), it makes more difficult to properly track (and animate) changes on an (immutable) item in a (immutable) collection.

The _key equality_ is a standardized way to determine that 2 objects that are not `Equals` are however representing the same entity.

For instance:

```csharp
public partial record Person(string Name, int Age);

var john1 = new Person("John Doe", 20);
var john2 = john1 with { Age=21 };

Console.WriteLine("Are the same : " + john1.Equals(john2));
Console.WriteLine("Are the same person : " + john1.KeyEquals(john2));
```

This would output:

```output
Are the same : false
Are the same person : true
```

In this example, the _key_ is the `Name`, and any instance of `Person` that `Name == "John Doe"` is considered to represent the same person,
no matter values of the other properties.

Then if you are dealing with list:

```csharp
var list1 = ImmutableList.Create(john1);
var list2 = list1.Replace(john1, john2);
```

When comparing the `list1` and the `list2` the `IKeyEquatable<T>` allows you to detect that the item is only a newer version of the same entity,
so visually we only need to update the current item and not animate the removal and then the add of the item.

The implementation of this concept is located in the `Uno.Extensions.Equality` namespace.

## IKeyEquatable<T>

This is like `IEquatable<T>` but specialized for the _key_ comparison.

When you implement this, you should compare only the keys of your object.

> [!TIP]
> You usually don't have to implement it by yourself, see [generation](#generation).

## KeyEqualityComparer

This is like the `EqualityComparer` but which relies on the `IKeyEquatable` instead of `IEquatable` to check equality.

> [!TIP]
> As all types are not necessarily implementing `IKeyEquatable<T>`, there is no equivalent to the `EqualityComparer<T>.Default`.
> You can however use the static `KeyEqualityComparer.Find<T>()` to dynamically get a _key equality_ comparer,
> if the given `T` does implement `IKeyEquatable<T>`.

## Generation

The `IKeyEquatable<T>` implementation is automatically generated for `partial record` that has a property named `Id` or `Key`.

### How to configure keys?

You have several way to configure the keys:

1. Add the `[Key]` attribute on properties that should be used for _key equality_.

    ```csharp
    public partial record MyItem(
        [property:Key] Guid EntityId,
        [property:Key] string SourceId,
        string Value);
    ```

    > [!IMPORTANT]
    > As soon as a property has been flagged with the `[Key]` attribute, the implicit keys are not used.
    >
    > [!NOTE]
    > This is the single way to have more than one property to compute _key equality_.
    >
    > [!NOTE]
    > You can use indifferently the `Uno.Extensions.Equality.KeyAttribute` or the `System.ComponentModel.DataAnnotations.KeyAttribute`

2. Add the `[ImplicitKeyEquality]` attribute on your record

    ```csharp
    [ImplicitKeyEquality("EntityId")]
    public partial record MyItem(Guid EntityId, string SourceId, string Value);
    ```

3. Change the default keys for the whole project by setting the `[ImplicitKeyEquality]` on the assembly:

    ```csharp
    [assembly:ImplicitKeyEquality("Id", "Key", "EntityId")]
    ```

    > [!IMPORTANT]
    > The generation of `IKeyEquatable<T>` using implicit keys will use only **one** matching property.
    > The properties a tested in the order in which they have been defined on the `[ImplicitKeyEquality]` attribute.
    > This means that in the example above, if a record have 2 properties `Id` and `EntityId`, only the property named `Id` will be used.

## How to disable generation?

You can disable the generation on a given type by adding `[ImplicitKeys(IsEnabled = false)]` on it.

To disable the generation for the whole project, set that attribute directly on the assembly:

```csharp
[assembly:ImplicitKeys(IsEnabled = false)]
```

> [!IMPORTANT]
> This disable only the generation based on implicit keys.
> If you have a record that has a property flagged with the `[Key]` attribute,
> the generator will still generate the `IKeyEquatable<T>` implementation for that type.
