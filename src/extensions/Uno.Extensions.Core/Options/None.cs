using System;
using System.CodeDom;
using System.Diagnostics;

namespace Uno.Extensions.Options;

/// <summary>
/// Special Option representing an absence of value.
/// </summary>
/// <typeparam name="T">The type of object that Option wraps</typeparam>
/// <remarks>
/// This is the implementation of a functional "Option Type" using F# semantic
/// https://en.wikipedia.org/wiki/Option_type
/// </remarks>
[DebuggerDisplay("None()")]
public sealed class None<T> : Option<T>
{
    /// <summary>
    /// Gets a singleton instance of this
    /// </summary>
#pragma warning disable CA1000 // Do not declare static members on generic types
    public static None<T> Instance { get; } = new None<T>();
#pragma warning restore CA1000 // Do not declare static members on generic types

    private None() : base(OptionType.None)
    {
    }

    public override object GetValue() => throw new NotSupportedException("Cannot get value on a None");

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is None<T>;

    /// <inheritdoc/>
    public override int GetHashCode() => typeof(T).GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"None<{typeof(T).Name}>()";
}
