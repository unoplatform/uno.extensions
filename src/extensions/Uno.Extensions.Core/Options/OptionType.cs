namespace Uno.Extensions.Options;

/// <summary>
/// Represents the different possible types of an <see cref="Option{T}"/>
/// </summary>
public enum OptionType : byte
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
