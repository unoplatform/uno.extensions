using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Uno.Extensions.Validation;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// Adapters of the validators accepted by the validation APIs of MVUX (cf. State.Validate and the Validation of commands).
/// </summary>
internal static class ValidationHelper
{
	/// <summary>
	/// Materializes validation results, ignoring null results (i.e. <see cref="ValidationResult.Success"/>).
	/// </summary>
	/// <returns>The results, or null if <paramref name="results"/> is null.</returns>
	[return: NotNullIfNotNull(nameof(results))]
	public static IImmutableList<ValidationResult>? ToResults(IEnumerable<ValidationResult?>? results)
		=> results switch
		{
			null => null,
			IImmutableList<ValidationResult> list when !list.Contains(null!) => list,
			_ => results.OfType<ValidationResult>().ToImmutableList(),
		};

	/// <summary>
	/// Adapts a validator which returns the error message of an invalid value (null or empty when valid).
	/// </summary>
	/// <remarks>The error is reported for the validated value itself (its <see cref="ValidationResult.MemberNames"/> is empty).</remarks>
	public static AsyncFunc<T, IEnumerable<ValidationResult>> FromErrorMessage<T>(AsyncFunc<T, string?> getErrorMessage)
		=> async (value, ct) => await getErrorMessage(value, ct).ConfigureAwait(false) is { Length: > 0 } message
			? [new ValidationResult(message)]
			: Array.Empty<ValidationResult>();

	/// <summary>
	/// Adapts a predicate (true when the value is valid) and the error to report when the value is not valid.
	/// </summary>
	public static AsyncFunc<T, IEnumerable<ValidationResult>> FromPredicate<T>(AsyncFunc<T, bool> isValid, string error)
		=> FromErrorMessage<T>(async (value, ct) => await isValid(value, ct).ConfigureAwait(false) ? null : error);

	/// <summary>
	/// Adapts an <see cref="IValidator"/>.
	/// </summary>
	/// <remarks>A null value (e.g. a null command parameter provided by the view) has nothing to validate: it is considered as valid.</remarks>
	public static AsyncFunc<T, IEnumerable<ValidationResult>> FromValidator<T>(IValidator validator)
		where T : notnull
		=> (value, ct) => value is null
			? new(Array.Empty<ValidationResult>())
			: validator.ValidateAsync(value, null, ct);

	/// <summary>
	/// Wraps a validator so the error messages of its results are resolved as resource keys (if a <paramref name="localizer"/> is provided).
	/// </summary>
	public static AsyncFunc<T, IEnumerable<ValidationResult>> Localized<T>(
		this AsyncFunc<T, IEnumerable<ValidationResult>> validator,
		IStringLocalizer? localizer)
	{
		if (localizer is null)
		{
			return validator;
		}

		var resolver = new ValidationLocalizer(localizer);
		return async (value, ct) => resolver.Localize<T>(await validator(value, ct).ConfigureAwait(false));
	}
}
