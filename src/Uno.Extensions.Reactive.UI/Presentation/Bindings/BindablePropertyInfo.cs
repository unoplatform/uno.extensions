using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive;

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

	public void Subscribe(Action<T?> onPropertyChanged)
		=> _subscribeOwnerUpdated(onPropertyChanged);

	public ValueTask Update(Func<T?, T?> updater, bool isLeafPropertyChanged, CancellationToken ct)
		=> _update(updater, isLeafPropertyChanged, ct);
}
