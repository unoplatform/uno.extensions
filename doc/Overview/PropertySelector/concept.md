---
uid: Overview.PropertySelector.Concept
---
# Concept

The `PropertySelector` is a standardized way to declare a _path_ to a _value_ given from a root _entity_.
Using this _path_, an helper _IValueAccessor_ is being generated at compile time to give read and write access to the target _value_.

This is an helper to avoid the usage a 2 delegates (one for read, and one for write) to edit a _value_ on a given _entity_.

For instance, given a `Movie` record:
```csharp
public partial record Movie(int Likes);
```

And an helper class:
```csharp
public static class Math
{
	public T Increment<T>(T instance, PropertySelector<T, int> selector)
	{
		// ..
	}
}
```

You can do something like:
```
var current = new Movie(0);
var updated = Math.Increment(current, m => m.Likes);
```

In this example `current.Likes` will be `0` while `updated.Likes` is `1`.

## Limitations

1. Only records are supported.
2. You can use only property in the delegate. Methods, constant or any other constructs are not supported.
3. You cannot use method-group, only the lambda syntax is allowed.
4. You delegate cannot use any capture.

Basically you can only write something like `e => e.A.B.C` as `PropertySelector`.

## Declare a PropertySelector parameter on a method

To avoid usage of reflection at runtime, the `PropertySelector` relies on generated code to work.
Considering this we need to match an instance of a `PropertySelector` at runtime to its declaration using an identifier 
derived from information that are also avaliable at compile time.

To avoid to require from the end user to profide a such unique identifier, we are relying on the `[CallerFilePath]` and `[CallerLineNumber]` attributes.
When method parameters are flagged with those attributes, values are automatically full-filled by the compiler.

We are then using those `path` and `line` among the name of the `PropertySelector` argument to uniquely identify it, 
so you can resove the `IValueAccessor` using the `PropertySelectors.Get`:

```csharp
public T Increment(T entity, PropertySelector<T, int> selector, [CallerFilePath] string path = "", [CallerLineNumber] int line = -1)
{
	var accessor = PropertySelectors.Get(selector, nameof(selector), path, line);
	var currentValue = accessor.Get(entity);
	var updatedEntity = accessor.Set(entity, currentValue + 1);
	
	return updatedEntity;
```

> [!IMPORTANT]
> The `path` and `line` arguments must be resolvable at compile time to be able to compute the _key_.
> If a user wants to provide `path` and / or `line`, only constant values are allowed.

## Passing a PropertySelector between methods

You cannot pass a `PropertySelector` from a method to another one as it would require to provide the `path` and `line` 
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
