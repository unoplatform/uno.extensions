using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive;

public static class StateExtensions
{
	public static ValueTask UpdateValue<T>(this IState<T> state, Func<Option<T>, Option<T>> updater, CancellationToken ct)
		=> state.Update(m => m.With().Data(updater(m.Current.Data)), ct);

	public static ValueTask Set<T>(this IState<T> state, Option<T> value, CancellationToken ct)
		where T : struct
		=> state.Update(m => m.With().Data(value), ct);

	public static ValueTask Set(this IState<string> state, Option<string> value, CancellationToken ct)
		=> state.Update(m => m.With().Data(value), ct);
}
