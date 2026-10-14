using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;
using Uno.Extensions.Validation;

namespace Uno.Extensions.Reactive;

partial class State
{
	/// <summary>
	/// [DEPRECATED] Use UpdateMessageAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateMessageAsync")]
#endif
	public static ValueTask UpdateMessage<T>(this IState<T> state, Action<MessageBuilder<T>> updater, CancellationToken ct)
		=> state.UpdateMessageAsync(updater, ct);

	/// <summary>
	/// Updates the value of a state
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="updater">The update method to apply to the current value.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask UpdateAsync<T>(this IState<T> state, Func<T?, T?> updater, CancellationToken ct = default)
		where T : notnull
		=> state.UpdateMessageAsync(
			m =>
			{
				var updatedValue = updater(m.CurrentData.SomeOrDefault());
				var updatedData = updatedValue is null ? Option<T>.None() : Option.Some(updatedValue);

				m.Data(updatedData);
			},
			ct);

	/// <summary>
	/// Updates the value of a state
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="updater">The update method to apply to the current value.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask UpdateAsync<T>(this IState<T> state, Func<T?, T?> updater, CancellationToken ct = default)
		where T : struct
		=> state.UpdateMessageAsync(
			m =>
			{
				var updatedValue = updater(m.CurrentData.SomeOrDefault());
				var updatedData = updatedValue.HasValue ? Option.Some(updatedValue.Value) : Option<T>.None();

				m.Data(updatedData);
			},
			ct);

	/// <summary>
	/// Updates the value of a state
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="updater">The update method to apply to the current value.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask UpdateAsync<T>(this IState<T?> state, Func<T?, T?> updater, CancellationToken ct = default)
		where T : struct
		=> state.UpdateMessageAsync(
			m =>
			{
				var updatedValue = updater(m.CurrentData.SomeOrDefault());
				var updatedData = updatedValue.HasValue ? Option.Some<T?>(updatedValue.Value) : Option<T?>.None();

				m.Data(updatedData);
			},
			ct);

	/// <summary>
	/// [DEPRECATED] Use UpdateAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateAsync")]
#endif
	public static ValueTask Update<T>(this IState<T> state, Func<T?, T?> updater, CancellationToken ct)
		where T : notnull
		=> UpdateAsync(state, updater, ct);

	/// <summary>
	/// Updates the value of a state
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="updater">The update method to apply to the current value.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask UpdateDataAsync<T>(this IState<T> state, Func<Option<T>, Option<T>> updater, CancellationToken ct = default)
		=> state.UpdateMessageAsync(m => m.Data(updater(m.CurrentData)), ct);

	/// <summary>
	/// [DEPRECATED] Use UpdateAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateDataAsync")]
#endif
	public static ValueTask UpdateData<T>(this IState<T> state, Func<Option<T>, Option<T>> updater, CancellationToken ct)
		=> UpdateDataAsync(state, updater, ct);

	/// <summary>
	/// [DEPRECATED] Use UpdateDataAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use UpdateDataAsync")]
#endif
	public static ValueTask UpdateValue<T>(this IState<T> state, Func<Option<T>, Option<T>> updater, CancellationToken ct)
		=> UpdateDataAsync(state, updater, ct);

	/// <summary>
	/// Sets the value of a state
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="value">The value to set.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask SetAsync<T>(this IState<T> state, T? value, CancellationToken ct = default)
		where T : struct
		=> state.UpdateMessageAsync(m => m.Data(Option.SomeOrNone(value)), ct);

	/// <summary>
	/// Sets the value of a state
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="value">The value to set.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask SetAsync<T>(this IState<T?> state, T? value, CancellationToken ct = default)
		where T : struct
		=> state.UpdateMessageAsync(m => m.Data(Option.SomeOrNone<T?>(value)), ct);

	/// <summary>
	/// Sets the value of a state
	/// </summary>
	/// <param name="state">The state to update.</param>
	/// <param name="value">The value to set.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask SetAsync(this IState<string> state, string? value, CancellationToken ct = default)
		=> state.UpdateMessageAsync(m => m.Data(value is { Length: > 0 } ? value : Option<string>.None()), ct);

	/// <summary>
	/// [DEPRECATED] Use SetAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use SetAsync")]
#endif
	public static ValueTask Set<T>(this IState<T> state, T? value, CancellationToken ct)
		where T : struct
		=> SetAsync(state, value, ct);

	/// <summary>
	/// [DEPRECATED] Use SetAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use SetAsync")]
#endif
	public static ValueTask Set(this IState<string> state, string? value, CancellationToken ct)
		=> SetAsync(state, value, ct);

	/// <summary>
	/// [DEPRECATED] Use ForEach instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use ForEach")]
#endif
	public static IDisposable ForEachAsync<T>(this IState<T> state, AsyncAction<T?> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : notnull
		=> new StateForEach<T>(state, action.SomeOrNone(), $"ForEachAsync defined in {caller} at line {line}.");

	/// <summary>
	/// [DEPRECATED] Use ForEach instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use ForEach")]
#endif
	public static IDisposable ForEachAsync<T>(this IState<T?> state, AsyncAction<T?> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : struct
		=> new StateForEach<T?>(state, action.SomeOrNone(), $"ForEachAsync defined in {caller} at line {line}.");


	/// <summary>
	/// Execute an async callback each time the state is being updated.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to listen.</param>
	/// <param name="action">The callback to invoke on each update of the state.</param>
	/// <param name="caller"> For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <returns>An <see cref="IState"/> that can be used to chain other operations.</returns>
	public static IState<T> ForEach<T>(this IState<T> state, AsyncAction<T?> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : notnull
	{
		_ = AttachedProperty.GetOrCreate(
				owner: state,
				key: action,
				state: (caller, line),
				factory: static (s, a, d) => new StateForEach<T>(s, a.SomeOrNone(), $"ForEach defined in {d.caller} at line {d.line}."));

		return state;
	}

	/// <summary>
	/// Execute an async callback each time the state is being updated.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to listen.</param>
	/// <param name="action">The callback to invoke on each update of the state.</param>
	/// <param name="caller"> For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <param name="disposable"> A <see cref="IDisposable"/> that can be used to remove the callback registration.</param>
	/// <returns>An <see cref="IState"/> that can be used to chain other operations.</returns>
	public static IState<T> ForEach<T>(this IState<T> state, AsyncAction<T?> action, out IDisposable disposable, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : notnull
	{
		disposable = AttachedProperty.GetOrCreate(
						owner: state,
						key: action,
						state: (caller, line),
						factory: static (s, a, d) => new StateForEach<T>(s, a.SomeOrNone(), $"ForEach defined in {d.caller} at line {d.line}."));

		return state;
	}

	/// <summary>
	/// Execute an async callback each time the state is being updated.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to listen.</param>
	/// <param name="action">The callback to invoke on each update of the state.</param>
	/// <param name="caller"> For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <returns>A <see cref="IDisposable"/> that can be used to remove the callback registration.</returns>
	public static IState<T?> ForEach<T>(this IState<T?> state, AsyncAction<T?> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : struct
	{
		_ = AttachedProperty.GetOrCreate(
				owner: state,
				key: action,
				state: (caller, line),
				factory: static (s, a, d) => new StateForEach<T?>(s, a.SomeOrNone(), $"ForEach defined in {d.caller} at line {d.line}."));

		return state;
	}

	/// <summary>
	/// Execute an async callback each time the state is being updated.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to listen.</param>
	/// <param name="action">The callback to invoke on each update of the state.</param>
	/// <param name="caller"> For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>	
	/// <param name="disposable"> A <see cref="IDisposable"/> that can be used to remove the callback registration.</param>
	/// <returns>An <see cref="IState"/> that can be used to chain other operations.</returns>
	public static IState<T?> ForEach<T>(this IState<T?> state, AsyncAction<T?> action, out IDisposable disposable, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : struct
	{
		disposable = AttachedProperty.GetOrCreate(
						owner: state,
						key: action,
						state: (caller, line),
						factory: static (s, a, d) => new StateForEach<T?>(s, a.SomeOrNone(), $"ForEach defined in {d.caller} at line {d.line}."));

		return state;
	}


	/// <summary>
	/// [DEPRECATED] Use ForEachData instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use ForEachData")]
#endif
	public static IDisposable ForEachDataAsync<T>(this IState<T> state, AsyncAction<Option<T>> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		=> new StateForEach<T>(state, action, $"ForEachDataAsync defined in {caller} at line {line}.");

	/// <summary>
	/// Execute an async callback each time the state is being updated.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to listen.</param>
	/// <param name="action">The callback to invoke on each update of the state.</param>
	/// <param name="caller"> For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>		
	/// <returns>An <see cref="IState"/> that can be used to chain other operations.</returns>
	public static IState<T> ForEachData<T>(this IState<T> state, AsyncAction<Option<T>> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
	{
		_ = AttachedProperty.GetOrCreate(
				owner: state,
				key: action,
				state: (caller, line),
				factory: static (s, a, d) => new StateForEach<T>(s, a, $"ForEachData defined in {d.caller} at line {d.line}."));

		return state;
	}

	/// <summary>
	/// Execute an async callback each time the state is being updated.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to listen.</param>
	/// <param name="action">The callback to invoke on each update of the state.</param>
	/// <param name="disposable"> A <see cref="IDisposable"/> that can be used to remove the callback registration.</param>
	/// <param name="caller"> For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>	
	/// <returns>An <see cref="IState"/> that can be used to chain other operations.</returns>
	public static IState<T> ForEachData<T>(this IState<T> state, AsyncAction<Option<T>> action, out IDisposable disposable, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
	{
		disposable = AttachedProperty.GetOrCreate(
						owner: state,
						key: action,
						state: (caller, line),
						factory: static (s, a, d) => new StateForEach<T>(s, a, $"ForEachData defined in {d.caller} at line {d.line}."));

		return state;
	}

	/// <summary>
	/// [DEPRECATED] Use .ForEachAsync instead
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
#if DEBUG // To avoid usage in internal reactive code, but without forcing apps to update right away
	[Obsolete("Use ForEach")]
#endif
	public static IDisposable Execute<T>(this IState<T> state, AsyncAction<T?> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : notnull
	{
		_ = ForEachAsync(state, action, caller, line);

		return Disposable.Empty;
	}

	/// <summary>
	/// Validates the value of a state each time it changes, publishing the results on the <see cref="MessageAxis.Validation"/> of the state.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to validate.</param>
	/// <param name="validator">The async method which validates a value of the state.</param>
	/// <param name="localizer">An optional localizer: when provided, the <see cref="ValidationResult.ErrorMessage"/> of the results are resource keys resolved through it.</param>
	/// <returns>The given <paramref name="state"/>, so it can be used to chain other operations.</returns>
	/// <remarks>
	/// Validation never blocks a value: invalid values are still set on the state, validation results only annotate them.
	/// The validator is started on the thread pool each time the data changes (including the initial value) and the previous pending validation is cancelled.
	/// On single-threaded WebAssembly, the thread pool is the UI thread: avoid long synchronous work in the validator.
	/// The validator should honor its cancellation token: a validator which ignores it keeps running (its results are then discarded), so a slow validator (e.g. a remote call) should also apply its own timeout.
	/// Results produced for a value that is no longer the current value of the state are discarded.
	/// Results are kept until the next validation: they are not dropped when the source of the state produces the same value again, and they are not reported for another value.
	/// When the state has no value (None), the validation results are cleared. A null value is still a value: the validator is invoked with null.
	/// If the validator throws, the error is logged and the state is not set in error: the value has no results (the results of a previous value are not kept for it).
	/// This is idempotent: invoking this method multiple times on the same state replaces the validator (starting at the next data change).
	/// When a <paramref name="localizer"/> is provided, messages are resolved each time the validator produces results (i.e. using the culture at that time).
	/// A resolved result is a new <see cref="ValidationResult"/> with the resolved message and the same <see cref="ValidationResult.MemberNames"/> (a result of a type derived from <see cref="ValidationResult"/> is not kept as is).
	/// A key which is not found, or for which the localizer throws (the error is logged), keeps the result as produced by the validator. Resolved messages are not formatted.
	/// A lambda which only throws, or which only returns null or default, also matches <see cref="Validate{T}(IState{T}, AsyncFunc{T, string}, IStringLocalizer)"/> (CS0121): give it an explicit return type.
	/// </remarks>
	/// <exception cref="ArgumentNullException">If the <paramref name="state"/> or the <paramref name="validator"/> is null.</exception>
	/// <exception cref="NotSupportedException">If the <paramref name="state"/> has not been created using the MVUX State factories.</exception>
	public static IState<T> Validate<T>(this IState<T> state, AsyncFunc<T, IEnumerable<ValidationResult>> validator, IStringLocalizer? localizer = null)
	{
		ArgumentNullException.ThrowIfNull(validator);

		return Validate(state, validator, localizer, source: validator);
	}

	// The source is the user's validator from which the validator has been created, used to identify it in logs.
	private static IState<T> Validate<T>(IState<T> state, AsyncFunc<T, IEnumerable<ValidationResult>> validator, IStringLocalizer? localizer, object source)
	{
		ArgumentNullException.ThrowIfNull(state);

		if (state is not StateImpl<T> impl)
		{
			throw new NotSupportedException($"Validation is supported only on states created using the State factories (got '{state.GetType().Name}').");
		}

		impl.SetValidator(validator.Localized(localizer), source);

		return state;
	}

	/// <summary>
	/// Sets the validation results of a state, published on the <see cref="MessageAxis.Validation"/> of the state.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="results">The validation results of the current value of the state, null or empty to clear them.</param>
	/// <param name="ct">A token to abort the async operation.</param>
	/// <remarks>
	/// This is useful to validate on demand, for instance the whole form when the user clicks Save.
	/// The results are kept until they are replaced: by another call to this method, or, if a validator has been set using <c>Validate</c>, by its results on the next data change of the state.
	/// Unlike an update of the data, they are not dropped when the source of the state produces a new value.
	/// They also win over the results of a validation which was already running when this method is invoked.
	/// This works with any <see cref="IState{T}"/>: for a state which has not been created using the MVUX State factories, the results are set using <see cref="IState{T}.UpdateMessageAsync"/>
	/// (so they follow the update rules of that state).
	/// </remarks>
	public static ValueTask SetValidationAsync<T>(this IState<T> state, IEnumerable<ValidationResult>? results, CancellationToken ct = default)
	{
		ArgumentNullException.ThrowIfNull(state);
		ct.ThrowIfCancellationRequested();

		// Materialize the results here, so the update pipeline of the state does not enumerate the caller's sequence.
		var validation = ValidationHelper.ToResults(results) ?? ImmutableList<ValidationResult>.Empty;

		// A state created by the State factories publishes through its single entry point, like its validator and commands.
		return state is IValidationTarget target
			? target.PublishValidationAsync(validation, ct)
			: state.UpdateMessageAsync(msg => msg.Validation(validation), ct);
	}

	/// <summary>
	/// Validates the value of a state each time it changes, using a validator which returns the error message of an invalid value.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to validate.</param>
	/// <param name="validator">The async method which validates a value of the state, returning the error message (or its resource key when a <paramref name="localizer"/> is provided), or null (or empty) when the value is valid.</param>
	/// <param name="localizer">An optional localizer: when provided, the value returned by the <paramref name="validator"/> is a resource key resolved through it.</param>
	/// <returns>The given <paramref name="state"/>, so it can be used to chain other operations.</returns>
	/// <remarks>
	/// The error is reported for the state itself (its <see cref="ValidationResult.MemberNames"/> is empty).
	/// This has the same behavior as <see cref="Validate{T}(IState{T}, AsyncFunc{T, IEnumerable{ValidationResult}}, IStringLocalizer)"/>.
	/// A lambda which only throws, or which only returns null or default, also matches that overload (CS0121): give it an explicit return type.
	/// </remarks>
	/// <exception cref="ArgumentNullException">If the <paramref name="state"/> or the <paramref name="validator"/> is null.</exception>
	/// <exception cref="NotSupportedException">If the <paramref name="state"/> has not been created using the MVUX State factories.</exception>
	public static IState<T> Validate<T>(this IState<T> state, AsyncFunc<T, string?> validator, IStringLocalizer? localizer = null)
	{
		ArgumentNullException.ThrowIfNull(validator);

		return Validate(state, ValidationHelper.FromErrorMessage(validator), localizer, source: validator);
	}

	/// <summary>
	/// Validates the value of a state each time it changes, using a predicate and a fixed error.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to validate.</param>
	/// <param name="isValid">The async predicate which returns true when a value of the state is valid.</param>
	/// <param name="error">The error message (or its resource key when a <paramref name="localizer"/> is provided) reported when the value is not valid.</param>
	/// <param name="localizer">An optional localizer: when provided, <paramref name="error"/> is a resource key resolved through it.</param>
	/// <returns>The given <paramref name="state"/>, so it can be used to chain other operations.</returns>
	/// <remarks>
	/// The error is reported for the state itself (its <see cref="ValidationResult.MemberNames"/> is empty).
	/// This has the same behavior as <see cref="Validate{T}(IState{T}, AsyncFunc{T, IEnumerable{ValidationResult}}, IStringLocalizer)"/>.
	/// </remarks>
	/// <exception cref="ArgumentNullException">If the <paramref name="state"/> or <paramref name="isValid"/> is null.</exception>
	/// <exception cref="ArgumentException">If <paramref name="error"/> is null or empty.</exception>
	/// <exception cref="NotSupportedException">If the <paramref name="state"/> has not been created using the MVUX State factories.</exception>
	public static IState<T> Validate<T>(this IState<T> state, AsyncFunc<T, bool> isValid, string error, IStringLocalizer? localizer = null)
	{
		ArgumentNullException.ThrowIfNull(isValid);
		ArgumentException.ThrowIfNullOrEmpty(error);

		return Validate(state, ValidationHelper.FromPredicate(isValid, error), localizer, source: isValid);
	}

	/// <summary>
	/// Validates the value of a state each time it changes, using an <see cref="IValidator"/>.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to validate.</param>
	/// <param name="validator">The validator to use (e.g. the one registered by <c>UseValidation</c>).</param>
	/// <param name="localizer">An optional localizer: when provided, the <see cref="ValidationResult.ErrorMessage"/> of the results are resource keys resolved through it.</param>
	/// <returns>The given <paramref name="state"/>, so it can be used to chain other operations.</returns>
	/// <remarks>
	/// This has the same behavior as <see cref="Validate{T}(IState{T}, AsyncFunc{T, IEnumerable{ValidationResult}}, IStringLocalizer)"/>.
	/// </remarks>
	/// <exception cref="ArgumentNullException">If the <paramref name="state"/> or the <paramref name="validator"/> is null.</exception>
	/// <exception cref="NotSupportedException">If the <paramref name="state"/> has not been created using the MVUX State factories.</exception>
	public static IState<T> Validate<T>(this IState<T> state, IValidator validator, IStringLocalizer? localizer = null)
		where T : notnull
	{
		ArgumentNullException.ThrowIfNull(validator);

		return Validate(state, ValidationHelper.FromValidator<T>(validator), localizer, source: validator);
	}
}
