using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// Info about a property which can be backed by a <see cref="Bindable{T}"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct BindablePropertyInfo<T>
{
	private readonly IBindable _owner;
	private readonly string _name;
	private readonly Action<Action<T>> _subscribeOwnerUpdated;
	private readonly AsyncAction<Func<T, T>, bool>? _update;

	internal BindablePropertyInfo(
		IBindable owner,
		string name,
		(IFeed<T> feed, Action<Action<T>> syncUpdated) getter,
		AsyncAction<Func<T, T>, bool>? setter)
	{
		_owner = owner;
		_name = name;
		(Feed, _subscribeOwnerUpdated) = getter;
		_update = setter;
	}

	internal string Name => _name;

	internal bool IsValid => _owner is not null;

	internal bool CanWrite => _update is not null;

	internal IFeed<T> Feed { get; }

	/// <summary>
	/// Adds a callback which is invoked when the value of the property changed.
	/// </summary>
	/// <param name="onPropertyChanged">The callback to invoke</param>
	/// <remarks>The <paramref name="onPropertyChanged"/> callback will be invoked sync on subscribe, then it will be invoked on the UI thread only.</remarks>
	public void Subscribe(Action<T> onPropertyChanged)
		=> _subscribeOwnerUpdated(onPropertyChanged);

	/// <summary>
	/// Updates the property.
	/// </summary>
	/// <param name="updater">The method to update the current value.</param>
	/// <param name="isLeafPropertyChanged">Indicates if the update is directly on this property (true) or it's due to an update of a sub-property.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns></returns>
	/// <remarks>This method has to be invoked on the UI thread.</remarks>
	public async ValueTask Update(Func<T, T> updater, bool isLeafPropertyChanged, CancellationToken ct)
	{
		if (CanWrite)
		{
			await _update!(updater, isLeafPropertyChanged, ct).ConfigureAwait(false);
		}
	}
}
