---
uid: Uno.Extensions.PropertySelector.Concept
---
# Concept

The `PropertySelector` is a standardized way to declare a _path_ to a _value_ given from a root _entity_.
Using this _path_, an helper _IValueAccessor_ is being generated at compile time to give read and write access to the target _value_.

This is a helper to avoid the usage of 2 delegates (one for read, and one for write) to edit a _value_ on a given _entity_.

For instance, given a `Movie` record:

```csharp
public partial record Movie(int Likes);
```

And a helper class:

```csharp
public static class Math
{
    public T Increment<T>(T instance, PropertySelector<T, int> selector)
    {
        // ..
    }
}
```

You can allow the following usage:

```csharp
var current = new Movie(0);
var updated = Math.Increment(current, m => m.Likes);
```

In this example `current.Likes` will be `0` while `updated.Likes` is `1`.

## Limitations

1. Only records are supported.
2. Only properties are supported in the delegate. Methods, constants or any other constructs are not supported.
3. Only the lambda syntax is allowed, method groups are not supported.
4. The delegate cannot use any captured variables, fields or members.

In short, the only supported syntax is the following: `e => e.A.B.C` as `PropertySelector`.

## Declare a PropertySelector parameter on a method

To avoid the use of reflection at runtime, the `PropertySelector` relies on generated code.
Considering this, the generation tooling needs to match an instance of a `PropertySelector` at runtime to its declaration using an identifier derived from information that are also available at compile time.

To avoid requiring from the end user to provide a unique identifier, the generation tooling relies on the `[CallerFilePath]` and `[CallerLineNumber]` attributes.
When method parameters are flagged with those attributes, values are automatically populated by the compiler.

We are then using the `path` and `line` parameters along with the `PropertySelector` argument to uniquely identify it,
so it becomes possible to resolve the `IValueAccessor` using the `PropertySelectors.Get`:

```csharp
public T Increment(T entity, PropertySelector<T, int> selector, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1)
{
    var accessor = PropertySelectors.Get(selector, nameof(selector), path, line);
    var currentValue = accessor.Get(entity);
    var updatedEntity = accessor.Set(entity, currentValue + 1);

    return updatedEntity;
}
```

> [!IMPORTANT]
> The `path` and `line` arguments must be resolvable at compile time to be able to compute the _key_.
> If a user wants to provide `path` and / or `line`, only constant values are allowed.

## Providing a PropertySelector between methods

You cannot provide a `PropertySelector` from a method to another method as it would require to specify the `path` and `line`
as non constant parameters, which is not allowed.

You have to resolve the `IValueAccessor` and pass it as parameter.

```csharp
public T Increment(T entity, PropertySelector<T, int> selector, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1)
    => IncrementCore(entity, PropertySelectors.Get(selector, nameof(selector), path, line));

public T Increment(T entity, PropertySelector<T, int> selector, int by, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1)
    => IncrementCore(entity, PropertySelectors.Get(selector, nameof(selector), path, line));

private T IncrementCore<T>(T entity, IValueAccessor<T, int> accessor, int by)
    => accessor.Set(entity, accessor.Get(entity) + by)
```
