using System;

namespace Uno.Extensions.Options;

/// <summary>
/// Extension methods over <see cref="Option{T}"/>.
/// </summary>
public static class OptionExtensions
{
    /// <summary>
    /// Creates an <see cref="Option{T2}"/> using an <see cref="Option{T1}"/>.
    /// </summary>
    /// <typeparam name="T1">Type of the source option</typeparam>
    /// <typeparam name="T2">Type of the target option</typeparam>
    /// <param name="option">The source option</param>
    /// <param name="func">Method to create an <see cref="Option{T1}"/> for a given <typeparamref name="T1"/>.</param>
    /// <returns>The resulting option</returns>
    public static Option<T2> Bind<T1, T2>(this Option<T1> option, Func<T1?, Option<T2>> func)
    {
        T1? value1;
        return option.MatchSome(out value1)
            ? func(value1)
            : Option.None<T2>();
    }

    /// <summary>
    /// Convert an <see cref="Option{T1}"/> to an <see cref="Option{T2}"/>
    /// </summary>
    /// <typeparam name="T1">Type of the source option</typeparam>
    /// <typeparam name="T2">Type of the target option</typeparam>
    /// <param name="option">The source option to convert</param>
    /// <param name="func">Method to convert the value of the source to the value the target</param>
    /// <returns>The converted option</returns>
    public static Option<T2> Map<T1, T2>(this Option<T1> option, Func<T1?, T2> func)
    {
        T1? value1;
        return option.MatchSome(out value1)
            ? Option.Some(func(value1))
            : Option.None<T2>();
    }

    /// <summary>
    /// Gets the value of the option or default(<typeparamref name="T"/>) if none.
    /// </summary>
    /// <typeparam name="T">Type of the option</typeparam>
    /// <param name="option">The source option from which the value have to be extracted</param>
    /// <param name="defaultValue">The default value to use when none</param>
    /// <returns>The value of the option or default(<typeparamref name="T"/>) if none.</returns>
    public static T? SomeOrDefault<T>(this Option<T> option, T? defaultValue = default(T))
        => option.MatchSome(out var value)
            ? value
            : defaultValue;

    /// <summary>
    /// Gets the value of the option or default(object) if none.
    /// </summary>
    /// <param name="option">The source option from which the value have to be extracted</param>
    /// <param name="defaultValue">The default value to use when none</param>
    /// <returns>The value of the option or default(object) if none.</returns>
    public static object? SomeOrDefault(this Option option, object? defaultValue = null)
        => option.MatchSome(out var value)
            ? value
            : defaultValue;
}
