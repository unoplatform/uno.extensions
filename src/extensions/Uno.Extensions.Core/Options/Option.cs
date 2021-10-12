namespace Uno.Extensions.Options;

/// <summary>
/// Static method to create an <see cref="Option{T}"/>
/// </summary>
public abstract class Option
{
    /// <summary>
    /// Creates an option which represent an absence of value.
    /// </summary>
    /// <typeparam name="T">The type of entity to wrap</typeparam>
    /// <returns>An option that doesn't wrap a value</returns>
    public static Option<T> None<T>() => Options.None<T>.Instance;

    /// <summary>
    /// Creates an option for a given value.
    /// </summary>
    /// <typeparam name="T">The type of entity to wrap</typeparam>
    /// <param name="value">The value to wrap</param>
    /// <returns>An option that wraps some value</returns>
    public static Some<T> Some<T>(T value) => new Some<T>(value);

    protected internal Option(OptionType type)
    {
        Type = type;
    }

    public OptionType Type { get; }

    /// <summary>
    /// Gets a bool which indicates if this otion is <see cref="Some{T}"/> or not.
    /// </summary>
    /// <returns>True if this doesn't wrap a value (ie is none)</returns>
    public bool MatchNone()
    {
        return Type == OptionType.None;
    }

    /// <summary>
    /// Gets a bool which indicates if this otion is <see cref="Some{T}"/> or not
    /// </summary>
    /// <returns>True if this wraps a value</returns>
    public bool MatchSome()
    {
        return Type == OptionType.Some;
    }

    /// <summary>
    /// Gets a bool which indicates if this otion is <see cref="Some{T}"/> or not and send back the value.
    /// </summary>
    /// <param name="value">Returns the current value</param>
    /// <returns>True if this matches the value</returns>
    public bool MatchSome(out object value)
    {
        value = Type == OptionType.Some ? GetValue() : default(object);

        return Type == OptionType.Some;
    }

    protected abstract object GetValue();
}

/// <summary>
/// This is a base class for an option.
/// </summary>
/// <remarks>
/// This is the implementation of a functional "Option Type" using F# semantic
/// https://en.wikipedia.org/wiki/Option_type
/// </remarks>
/// <typeparam name="T">The type of entity to wrap</typeparam>
public abstract class Option<T> : Option
{
    protected Option(OptionType type)
        : base(type)
    {
    }

    /// <summary>
    /// Gets a bool which indicates if this otion is <see cref="Some{T}"/> or not and send back the value.
    /// </summary>
    /// <param name="value">Returns the value</param>
    /// <returns>True if this wraps a value</returns>
    public bool MatchSome(out T value)
    {
        value = Type == OptionType.Some ? (T)GetValue() : default(T);

        return Type == OptionType.Some;
    }

    /// <summary>
    /// Implicit conversion from <see cref="Option{T}"/> to T.
    /// </summary>
    /// <remarks>
    /// `null` or `None` will become `default(T)`.
    /// </remarks>
    /// <param name="o">The option to cast to <typeparamref name="T"/></param>
    public static implicit operator T(Option<T> o)
    {
        if (o == null || o.MatchNone())
        {
            return default(T);
        }
        return ((Some<T>)o).Value;
    }

    /// <summary>
    /// Implicit conversion of T to <see cref="Some{T}"/>
    /// </summary>
    /// <param name="o">The value to convert to an Option</param>
    public static implicit operator Option<T>(T o)
    {
        return Some(o);
    }
}
