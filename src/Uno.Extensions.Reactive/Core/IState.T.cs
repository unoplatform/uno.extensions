using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive;

/// <summary>
/// An <see cref="IFeed{T}"/> that is **state full** which:<br />
/// 1. replays the internal current data if any at the beginning of the enumeration;<br />
/// 2. can be updated
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public interface IState<T> : IFeed<T>, IState
{
#pragma warning disable CS1574 // XML comment has cref attribute that could not be resolved : Confusion non resolvable by code between notnull and struct. Can be resolved by user in VS.
#pragma warning disable CS0419 // Ambiguous reference in cref attribute
	/// <summary>
	/// Updates the current internal message.
	/// </summary>
	/// <param name="updater">The update method to apply to the current message.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns>A ValueTask to track the async update.</returns>
	/// <remarks>This is the raw way to update a state, you should consider using the <see cref="State.UpdateAsync{T}"/> method instead.</remarks>
#pragma warning restore CS1574 // XML comment has cref attribute that could not be resolved
#pragma warning restore CS0419 // Ambiguous reference in cref attribute
	ValueTask UpdateMessageAsync(Action<MessageBuilder<T>> updater, CancellationToken ct = default);
}
