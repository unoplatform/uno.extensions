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
	private readonly Action<Action<T?>> _subscribeOwnerUpdated;
	private readonly AsyncAction<Func<T?, T?>, bool> _update;

	internal BindablePropertyInfo(
		IBindable owner,
		string name,
		Action<Action<T?>> subscribeOwnerUpdated,
		AsyncAction<Func<T?, T?>, bool> updateOwner)
	{
		_owner = owner;
		_name = name;
		_subscribeOwnerUpdated = subscribeOwnerUpdated;
		_update = updateOwner;
	}

	internal bool IsValid => _owner is not null;

	/// <summary>
	/// Adds a callback which is invoked when the value of the property changed.
	/// </summary>
	/// <param name="onPropertyChanged"></param>
	public void Subscribe(Action<T?> onPropertyChanged)
		=> _subscribeOwnerUpdated(onPropertyChanged);

	/// <summary>
	/// Updates the property.
	/// </summary>
	/// <param name="updater">The method to update the current value.</param>
	/// <param name="isLeafPropertyChanged">Indicates if the update is directly on this property (true) or it's dure to an update of a sub-property.</param>
	/// <param name="ct">A cancellation to cancel the async operation.</param>
	/// <returns></returns>
	public ValueTask Update(Func<T?, T?> updater, bool isLeafPropertyChanged, CancellationToken ct)
		=> _update(updater, isLeafPropertyChanged, ct);
}
