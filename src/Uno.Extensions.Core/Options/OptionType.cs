namespace Uno.Extensions.Options;

/// <summary>
/// Represents the different possible types of an <see cref="Option{T}"/>
/// </summary>
#pragma warning disable CA1028 // Enum Storage should be Int32
public enum OptionType : byte
#pragma warning restore CA1028 // Enum Storage should be Int32
{
    /// <summary>
    /// The option does not have value
    /// </summary>
    None = 0,

    /// <summary>
    /// The option have a value
    /// </summary>
    Some = 1
}
