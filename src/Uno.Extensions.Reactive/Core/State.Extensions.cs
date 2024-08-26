using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
	/// Execute an async callback each time the state is being updated.
	/// </summary>
	/// <typeparam name="T">The type of the state</typeparam>
	/// <param name="state">The state to listen.</param>
	/// <param name="action">The callback to invoke on each update of the state.</param>
	/// <param name="caller"> For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <param name="line">For debug purposes, the name of this subscription. DO NOT provide anything here, let the compiler fulfill this.</param>
	/// <returns>A <see cref="IDisposable"/> that can be used to remove the callback registration.</returns>
	public static IDisposable ForEachDataAsync<T>(this IState<T> state, AsyncAction<Option<T>> action, [CallerMemberName] string? caller = null, [CallerLineNumber] int line = -1)
		=> new StateForEach<T>(state, action, $"ForEachDataAsync defined in {caller} at line {line}.");

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
}
