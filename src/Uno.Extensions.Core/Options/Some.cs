using System.Diagnostics;

namespace Uno.Extensions.Options;

/// <summary>
/// Option holding a value.
/// </summary>
/// <remarks>
/// This is the implementation of a functional "Option Type" using F# semantic
/// https://en.wikipedia.org/wiki/Option_type
/// </remarks>
/// <typeparam name="T">The type of entity to wrap</typeparam>
[DebuggerDisplay("Some({" + nameof(Value) + "})")]
public sealed class Some<T> : Option<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Some{T}"/> class for a given value.
    /// </summary>
    /// <param name="value">The value hold by the option</param>
    public Some(T value)
        : base(OptionType.Some)
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the value
    /// </summary>
#pragma warning disable CA1721 // Property names should not match get methods
    public T Value { get; }
#pragma warning restore CA1721 // Property names should not match get methods

    public override object? GetValue() => Value;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals((obj is Some<T> tobj) ? tobj : default(Some<T>));

    /// <summary>
    /// Checks whether two objects are equal
    /// </summary>
    /// <param name="other">The entity to compare</param>
    /// <returns>True if both entities are equal</returns>
    private bool Equals(Some<T>? other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (Type != other.Type)
        {
            return false;
        }

        if (ReferenceEquals(Value, other.Value))
        {
            return true;
        }

        if (Value == null || other.Value == null)
        {
            return false;
        }

        return Value.Equals(other.Value);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            return
                (typeof(T).GetHashCode() << 16) +
                (Value?.GetHashCode() ?? 0);
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"Some<{typeof(T).Name}>({Value})";

    /// <summary>
    /// Implicit conversion of T to <see cref="Some{T}"/>
    /// </summary>
    /// <param name="o">The object to wrap</param>
#pragma warning disable CA2225 // Operator overloads have named alternates
    public static implicit operator Some<T>(T o) => Some(o);
#pragma warning restore CA2225 // Operator overloads have named alternates
}
