using System;
using System.CodeDom;
using System.Diagnostics;

namespace Uno.Extensions.Options;

/// <summary>
/// Special Option representing an absence of value.
/// </summary>
/// <remarks>
/// This is the implementation of a functional "Option Type" using F# semantic
/// https://en.wikipedia.org/wiki/Option_type
/// </remarks>
[DebuggerDisplay("None()")]
public sealed class None<T> : Option<T>
{
    /// <summary>
    /// Singleton instance of this
    /// </summary>
    public static None<T> Instance { get; } = new None<T>();

    private None() : base(OptionType.None) { }

    protected override object GetValue() => throw new NotSupportedException("Cannot get value on a None");

    /// <inheritdoc/>
    public override bool Equals(object obj) => obj is None<T>;

    /// <inheritdoc/>
    public override int GetHashCode() => typeof(T).GetHashCode();

    /// <inheritdoc />
    public override string ToString() => $"None<{typeof(T).Name}>()";
}
