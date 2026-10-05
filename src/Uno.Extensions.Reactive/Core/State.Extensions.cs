using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

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
	/// <returns>The given <paramref name="state"/>, so it can be used to chain other operations.</returns>
	/// <remarks>
	/// Validation never blocks a value: invalid values are still set on the state, validation results only annotate them.
	/// The validator runs on a background thread each time the data changes (including the initial value) and the previous pending validation is cancelled.
	/// Results produced for a value that is no longer the current value of the state are discarded.
	/// When the state has no value, the validation results are cleared.
	/// If the validator throws, the error is logged and the previous results are kept (the state is not set in error).
	/// This is idempotent: invoking this method multiple times on the same state replaces the validator (starting at the next data change).
	/// </remarks>
	/// <exception cref="NotSupportedException">If the <paramref name="state"/> has not been created using the MVUX State factories.</exception>
	public static IState<T> Validate<T>(this IState<T> state, Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> validator)
	{
		if (state is not StateImpl<T> impl)
		{
			throw new NotSupportedException($"Validation is supported only on states created using the State factories (got '{state.GetType().Name}').");
		}

		impl.SetValidator(validator ?? throw new ArgumentNullException(nameof(validator)));

		return state;
	}

	/// <summary>
	/// Validates the value of a state each time it changes, using a validator which returns the error message of an invalid value.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to validate.</param>
	/// <param name="validator">The async method which validates a value of the state, returning the error message, or null (or empty) when the value is valid.</param>
	/// <returns>The given <paramref name="state"/>, so it can be used to chain other operations.</returns>
	/// <remarks>
	/// The error is reported for the state itself (its <see cref="ValidationResult.MemberNames"/> is empty).
	/// This has the same behavior as <see cref="Validate{T}(IState{T}, Func{T, CancellationToken, ValueTask{IEnumerable{ValidationResult}}})"/>.
	/// </remarks>
	/// <exception cref="NotSupportedException">If the <paramref name="state"/> has not been created using the MVUX State factories.</exception>
	public static IState<T> Validate<T>(this IState<T> state, AsyncFunc<T, string?> validator)
	{
		ArgumentNullException.ThrowIfNull(validator);

		return state.Validate(ToValidator(validator));
	}

	/// <summary>
	/// Validates the value of a state each time it changes, using a predicate and a fixed error message.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to validate.</param>
	/// <param name="isValid">The async predicate which returns true when a value of the state is valid.</param>
	/// <param name="errorMessage">The error message reported when the value is not valid.</param>
	/// <returns>The given <paramref name="state"/>, so it can be used to chain other operations.</returns>
	/// <remarks>
	/// The error is reported for the state itself (its <see cref="ValidationResult.MemberNames"/> is empty).
	/// This has the same behavior as <see cref="Validate{T}(IState{T}, Func{T, CancellationToken, ValueTask{IEnumerable{ValidationResult}}})"/>.
	/// </remarks>
	/// <exception cref="NotSupportedException">If the <paramref name="state"/> has not been created using the MVUX State factories.</exception>
	public static IState<T> Validate<T>(this IState<T> state, AsyncFunc<T, bool> isValid, string errorMessage)
	{
		ArgumentNullException.ThrowIfNull(isValid);
		ArgumentException.ThrowIfNullOrEmpty(errorMessage);

		return state.Validate(ToValidator<T>(async (value, ct) => await isValid(value, ct).ConfigureAwait(false) ? null : errorMessage));
	}

	/// <summary>
	/// Validates the value of a state each time it changes, using a validator which returns the resource key of the error message of an invalid value.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to validate.</param>
	/// <param name="localizer">The localizer used to get the error message from the resource key.</param>
	/// <param name="validator">The async method which validates a value of the state, returning the resource key of the error message, or null (or empty) when the value is valid.</param>
	/// <returns>The given <paramref name="state"/>, so it can be used to chain other operations.</returns>
	/// <remarks>
	/// The message is resolved from the <paramref name="localizer"/> each time the validation fails (i.e. using the culture at that time).
	/// A key which is not found gives the key itself as message (as per the <see cref="IStringLocalizer"/> contract).
	/// The error is reported for the state itself (its <see cref="ValidationResult.MemberNames"/> is empty).
	/// This has the same behavior as <see cref="Validate{T}(IState{T}, Func{T, CancellationToken, ValueTask{IEnumerable{ValidationResult}}})"/>.
	/// </remarks>
	/// <exception cref="NotSupportedException">If the <paramref name="state"/> has not been created using the MVUX State factories.</exception>
	public static IState<T> Validate<T>(this IState<T> state, IStringLocalizer localizer, AsyncFunc<T, string?> validator)
	{
		ArgumentNullException.ThrowIfNull(localizer);
		ArgumentNullException.ThrowIfNull(validator);

		return state.Validate(ToValidator<T>(async (value, ct) =>
			await validator(value, ct).ConfigureAwait(false) is { Length: > 0 } key
				? localizer[key].Value
				: null));
	}

	/// <summary>
	/// Validates the value of a state each time it changes, using a predicate and the resource key of the error message.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to validate.</param>
	/// <param name="localizer">The localizer used to get the error message from the resource key.</param>
	/// <param name="isValid">The async predicate which returns true when a value of the state is valid.</param>
	/// <param name="errorMessageKey">The resource key of the error message reported when the value is not valid.</param>
	/// <returns>The given <paramref name="state"/>, so it can be used to chain other operations.</returns>
	/// <remarks>
	/// The message is resolved from the <paramref name="localizer"/> each time the validation fails (i.e. using the culture at that time).
	/// A key which is not found gives the key itself as message (as per the <see cref="IStringLocalizer"/> contract).
	/// The error is reported for the state itself (its <see cref="ValidationResult.MemberNames"/> is empty).
	/// This has the same behavior as <see cref="Validate{T}(IState{T}, Func{T, CancellationToken, ValueTask{IEnumerable{ValidationResult}}})"/>.
	/// </remarks>
	/// <exception cref="NotSupportedException">If the <paramref name="state"/> has not been created using the MVUX State factories.</exception>
	public static IState<T> Validate<T>(this IState<T> state, IStringLocalizer localizer, AsyncFunc<T, bool> isValid, string errorMessageKey)
	{
		ArgumentNullException.ThrowIfNull(localizer);
		ArgumentNullException.ThrowIfNull(isValid);
		ArgumentException.ThrowIfNullOrEmpty(errorMessageKey);

		return state.Validate(ToValidator<T>(async (value, ct) =>
			await isValid(value, ct).ConfigureAwait(false)
				? null
				: localizer[errorMessageKey].Value));
	}

	private static Func<T, CancellationToken, ValueTask<IEnumerable<ValidationResult>>> ToValidator<T>(AsyncFunc<T, string?> getErrorMessage)
		=> async (value, ct) => await getErrorMessage(value, ct).ConfigureAwait(false) is { Length: > 0 } message
			? [new ValidationResult(message)]
			: Array.Empty<ValidationResult>();
}
