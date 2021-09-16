using System.Collections;
using System.Collections.Generic;

namespace Uno.Extensions.Options;

/// <summary>
/// This is an implementation of <see cref="IEqualityComparer{T}"/> which compare the
/// <see cref="Option{T}"/> type and uses an optional inner comparer for the value.
/// </summary>
public sealed class OptionEqualityComparer<T> : IEqualityComparer<Option<T>>
{
    private readonly IEqualityComparer<T> _innerComparer;

    public OptionEqualityComparer(IEqualityComparer<T> innerComparer = null)
    {
        _innerComparer = innerComparer ?? EqualityComparer<T>.Default;
    }

    /// <inheritdoc />
    public bool Equals(Option<T> x, Option<T> y)
    {
        // treat any "null" as Option.None (that's not the job of this comparer to crash on this)
        x = x ?? Option.None<T>();
        y = y ?? Option.None<T>();

        if (x.Type != y.Type)
        {
            // One is None and other is Some
            // So definitely not equal!
            return false;
        }

        return x.Type == OptionType.None // both "None"
            || _innerComparer.Equals((x as Some<T>).Value, (y as Some<T>).Value); // compare the 2 "Some"
    }

    /// <inheritdoc />
    public int GetHashCode(Option<T> obj)
    {
        return (obj as Some<T>)?.Value?.GetHashCode() ?? 0;
    }
}
