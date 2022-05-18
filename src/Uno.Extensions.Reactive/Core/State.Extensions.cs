using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive;

partial class State
{
	/// <summary>
	/// Updates the value of a state
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="updater">The update method to apply to the current value.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask Update<T>(this IState<T> state, Func<T?, T?> updater, CancellationToken ct)
		where T : notnull
		=> state.UpdateMessage(
			m =>
			{
				var updatedValue = updater(m.Current.Data.SomeOrDefault());
				var updatedData = updatedValue is null ? Option<T>.None() : Option.Some(updatedValue);

				return m.With().Data(updatedData);
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
	public static ValueTask UpdateData<T>(this IState<T> state, Func<Option<T>, Option<T>> updater, CancellationToken ct)
		=> state.UpdateMessage(m => m.With().Data(updater(m.Current.Data)), ct);

	/// <summary>
	/// Sets the value of a state
	/// </summary>
	/// <typeparam name="T">Type of the value of the state.</typeparam>
	/// <param name="state">The state to update.</param>
	/// <param name="value">The value to set.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask Set<T>(this IState<T> state, T? value, CancellationToken ct)
		where T : struct
		=> state.UpdateMessage(m => m.With().Data(value is null ? Option<T>.None() : Option.Some(value.Value)), ct);

	/// <summary>
	/// Sets the value of a state
	/// </summary>
	/// <param name="state">The state to update.</param>
	/// <param name="value">The value to set.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	public static ValueTask Set(this IState<string> state, string? value, CancellationToken ct)
		=> state.UpdateMessage(m => m.With().Data(value is { Length: > 0 } ? value : Option<string>.None()), ct);

	/// <summary>
	/// Execute an async callback each time the state is being updated.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to listen.</param>
	/// <param name="action">The callback to invoke on each update of the state.</param>
	/// <param name="caller"> For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <returns>A <see cref="IDisposable"/> that can be used to remove the callback registration.</returns>
	public static IDisposable Execute<T>(this IState<T> state, AsyncAction<T?> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		where T : notnull
		=> new StateExecute<T>(state, action, $"Execute defined in {caller} at line {line}.");
}
