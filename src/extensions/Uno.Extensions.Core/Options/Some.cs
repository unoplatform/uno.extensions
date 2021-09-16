using System.Diagnostics;

namespace Uno.Extensions.Options;

/// <summary>
/// Option holding a value.
/// </summary>
/// <remarks>
/// This is the implementation of a functional "Option Type" using F# semantic
/// https://en.wikipedia.org/wiki/Option_type
/// </remarks>
[DebuggerDisplay("Some({" + nameof(Value) + "})")]
public sealed class Some<T> : Option<T>
{
    /// <summary>
    /// Creates an <see cref="Option{T}"/> for a given value
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
    public T Value { get; }

    protected override object GetValue() => Value;

    /// <inheritdoc/>
    public override bool Equals(object obj) => Equals(obj as Some<T>);

    /// <inheritdoc/>
    private bool Equals(Some<T> other)
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
                typeof(T).GetHashCode() << 16 +
                (Value?.GetHashCode() ?? 0);
        }
    }

    /// <inheritdoc />
    public override string ToString() => $"Some<{typeof(T).Name}>({Value})";

    /// <summary>
    /// Implicit conversion of T to <see cref="Some{T}"/>
    /// </summary>
    public static implicit operator Some<T>(T o) => Some(o);
}
