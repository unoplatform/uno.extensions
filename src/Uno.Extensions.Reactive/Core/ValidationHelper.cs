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
	public static Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> FromErrorMessage<T>(AsyncFunc<T, string?> getErrorMessage)
		=> async (value, ct) => await getErrorMessage(value, ct).ConfigureAwait(false) is { Length: > 0 } message
			? [new ValidationResult(message)]
			: Array.Empty<ValidationResult>();

	/// <summary>
	/// Adapts a predicate (true when the value is valid) and the error to report when the value is not valid.
	/// </summary>
	public static Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> FromPredicate<T>(AsyncFunc<T, bool> isValid, string error)
		=> FromErrorMessage<T>(async (value, ct) => await isValid(value, ct).ConfigureAwait(false) ? null : error);

	/// <summary>
	/// Adapts an <see cref="IValidator"/>.
	/// </summary>
	/// <remarks>A null value (e.g. a null command parameter provided by the view) has nothing to validate: it is considered as valid.</remarks>
	public static Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> FromValidator<T>(IValidator validator)
		where T : notnull
		=> (value, ct) => value is null
			? new(Array.Empty<ValidationResult>())
			: validator.ValidateAsync(value, null, ct);

	/// <summary>
	/// Wraps a validator so the error messages of its results are resolved as resource keys (if a <paramref name="localizer"/> is provided).
	/// </summary>
	public static Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> Localized<T>(
		this Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator,
		IStringLocalizer? localizer)
		=> localizer is null
			? validator
			: async (value, ct) => Localize(await validator(value, ct).ConfigureAwait(false), localizer);

	/// <summary>
	/// Resolves the error message of each result as a resource key.
	/// </summary>
	/// <remarks>
	/// This is materialized (not lazy) so the localizer is invoked once per result, on the validator thread, and not each time the results are enumerated.
	/// Results whose key is not found are kept as is: the value of a not found string is not reliable (e.g. the ResourceLoaderStringLocalizer replaces '.' by '/' in keys).
	/// </remarks>
	private static IEnumerable<ValidationResult> Localize(IEnumerable<ValidationResult>? results, IStringLocalizer localizer)
	{
		if (results is null)
		{
			return Array.Empty<ValidationResult>();
		}

		var localized = new List<ValidationResult>();
		foreach (var result in results)
		{
			if (result is null)
			{
				continue; // ValidationResult.Success
			}

			localized.Add(result is { ErrorMessage: { Length: > 0 } key } && localizer[key] is { ResourceNotFound: false } message
				? new ValidationResult(message.Value, result.MemberNames)
				: result);
		}

		return localized;
	}
}
