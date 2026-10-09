using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Uno.Extensions.Reactive.Logging;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// Resolves the error message of validation results as resource keys.
/// </summary>
/// <remarks>
/// A resolved result is a new <see cref="ValidationResult"/> with the resolved message and the same <see cref="ValidationResult.MemberNames"/>.
/// Results whose key is not found are kept as is: the value of a not found string is not reliable (e.g. the ResourceLoaderStringLocalizer replaces '.' by '/' in keys).
/// If the localizer throws, the result is kept as is too (the failure is logged), so the other results of the validator are still reported.
/// </remarks>
internal sealed class ValidationLocalizer(IStringLocalizer localizer)
{
	private int _hasFailed;

	/// <summary>
	/// Resolves the error message of each result as a resource key.
	/// </summary>
	/// <remarks>
	/// This is materialized (not lazy) so the localizer is invoked once per result, on the validator thread, and not each time the results are enumerated.
	/// Null results (i.e. <see cref="ValidationResult.Success"/>) are dropped.
	/// The results are returned as is when no message is resolved, so a validator which reports no error (e.g. on each keystroke) does not allocate.
	/// </remarks>
	public IEnumerable<ValidationResult> Localize<T>(IEnumerable<ValidationResult?>? results)
	{
		if (results is null)
		{
			return Array.Empty<ValidationResult>();
		}

		var source = results as IReadOnlyList<ValidationResult?> ?? results.ToList();
		List<ValidationResult>? localized = null;
		for (var i = 0; i < source.Count; i++)
		{
			var result = source[i];
			var resolved = result is { ErrorMessage: { Length: > 0 } key } && Resolve<T>(key) is { } message
				? new ValidationResult(message, result.MemberNames)
				: result;
			if (localized is null)
			{
				if (resolved is not null && ReferenceEquals(resolved, result))
				{
					continue;
				}

				// Allocated only once a result has to be replaced or dropped, with the results kept so far.
				localized = [.. source.Take(i).OfType<ValidationResult>()];
			}

			if (resolved is not null)
			{
				localized.Add(resolved);
			}
		}

		return localized ?? (IEnumerable<ValidationResult>)source;
	}

	private string? Resolve<T>(string key)
	{
		try
		{
			return localizer[key] is { ResourceNotFound: false } message ? message.Value : null;
		}
		catch (Exception error)
		{
			LogFailure<T>(error);
			return null;
		}
	}

	/// <remarks>
	/// The details of the error are logged only at the debug level, like the failures of validators.
	/// A validator runs on each change of the value (e.g. each keystroke), so only the first failure is logged at the error level, the next ones at the debug level.
	/// </remarks>
	private void LogFailure<T>(Exception error)
	{
		var log = this.Log();
		var level = Interlocked.Exchange(ref _hasFailed, 1) is 0 ? LogLevel.Error : LogLevel.Debug;
		if (log.IsEnabled(level))
		{
			log.Log(level, "The localizer '{Localizer}' failed with an '{ErrorType}' to resolve a validation message of a state of '{ValueType}'. The message is reported as produced by the validator.", localizer.GetType().Name, error.GetType().Name, typeof(T).Name);
		}
		if (log.IsEnabled(LogLevel.Debug))
		{
			log.LogDebug(error, "Localization failure details for the localizer '{Localizer}'.", localizer.GetType().Name);
		}
	}
}
