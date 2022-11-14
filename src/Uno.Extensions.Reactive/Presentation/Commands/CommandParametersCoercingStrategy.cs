using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive.Commands;

/// <summary>
/// Defines a strategy used in by a <see cref="CommandConfig"/> to coerce the parameters that are coming from the `CommandParameter` of the view,
/// and the configured "external" <see cref="CommandConfig.Parameter"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)] // This should be used only by code gen
public sealed class CommandParametersCoercingStrategy
{
	/// <summary>
	/// Strategy which ignores both parameters.
	/// Coercing always succeed.
	/// </summary>
	public static CommandParametersCoercingStrategy UseNoParameter { get; } = new((vp, ep) => (true, null));

	/// <summary>
	/// Strategy which will ignore the external parameter and use only the view parameter.
	/// Coercing will succeed as soon as the view parameter is set (i.e. Some).
	/// </summary>
	public static CommandParametersCoercingStrategy UseViewParameterOnly { get; } = new((vp, ep) => (vp.IsSome(out var value), value));

	/// <summary>
	/// Strategy which will ignore the view parameter and use only the external parameter.
	/// Coercing will succeed as soon as the external parameter is set (i.e. Some).
	/// </summary>
	public static CommandParametersCoercingStrategy UseExternalParameterOnly { get; } = new((vp, ep) => (ep.IsSome(out var value), value));

	/// <summary>
	/// Strategy which requires that both parameters are not set.
	/// Coercing always succeed if both view and external parameters are not set (i.e. Undefined of None).
	/// </summary>
	public static CommandParametersCoercingStrategy AllowNoParameter { get; } = new((vp, ep) => (IsNotSet(vp) && IsNotSet(ep), null));

	/// <summary>
	/// Strategy which will use only the view parameter and requires that the external parameter is not set.
	/// Coercing will succeed if the view parameters is set (i.e. Some) and the external parameter is not set (i.e. Undefined or None).
	/// </summary>
	public static CommandParametersCoercingStrategy AllowOnlyViewParameter { get; } = new((vp, ep) => (vp.IsSome(out var value) && IsNotSet(ep), value));

	/// <summary>
	/// Strategy which will require that the view parameter is not set and use only the external parameter.
	/// Coercing will succeed if the view parameters is not set (i.e. Undefined or None) and the external parameter is set (i.e. Some).
	/// </summary>
	public static CommandParametersCoercingStrategy AllowOnlyExternalParameter { get; } = new((vp, ep) => (ep.IsSome(out var value) && IsNotSet(vp), value));

	/// <summary>
	/// Creates a strategy that requires that both view and external parameters are set (i.e. Some).
	/// </summary>
	/// <param name="selector">Effective coercing method for the parameters.</param>
	/// <returns>A strategy that will use the provided selector to coerce the command parameter for <see cref="CommandConfig.CanExecute"/> and <see cref="CommandConfig.Execute"/> methods.</returns>
	public static CommandParametersCoercingStrategy UseBoth(Func<object?, object?, object?> selector)
		=> new((vp, ep) =>
		{
			if (!vp.IsSome(out var vpValue) || !ep.IsSome(out var epValue))
			{
				return (false, null);
			}

			return (true, selector(vpValue, epValue));
		});

	private readonly Func<Option<object?>, Option<object?>, (bool, object?)> _coerce;

	private CommandParametersCoercingStrategy(Func<Option<object?>, Option<object?>, (bool, object?)> coerce)
	{
		_coerce = coerce;
	}

	/// <summary>
	/// Coerce the view and the external parameter of a command.
	/// </summary>
	/// <param name="viewParameter">The parameter that is provided by the view (i.e. the `CommandParameter` property defined in the view).</param>
	/// <param name="externalParameter">The value of the parameter resolved using the <see cref="CommandConfig.Parameter"/>.</param>
	/// <param name="parameter">If coercion succeed, the value of the parameter that should be used for <see cref="CommandConfig.CanExecute"/> and <see cref="CommandConfig.Execute"/> methods.</param>
	/// <returns>True if the coercion succeed, false otherwise.</returns>
	public bool TryCoerce(Option<object?> viewParameter, Option<object?> externalParameter, out object? parameter)
	{
		(var result, parameter) = _coerce(viewParameter, externalParameter);
		return result;
	}

	private static bool IsNotSet(Option<object?> parameter)
		=> !parameter.IsSome(out var value) || value is null;
}
