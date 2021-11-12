using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Logging;

namespace Uno.Extensions.Reactive;

public readonly struct BindablePropertyInfo<T>
{
	private readonly IBindable _owner;
	private readonly string _name;
	private readonly Action<Action<T?>> _subscribeOwnerUpdated;
	private readonly ActionAsync<Func<T?, T?>> _update;

	internal BindablePropertyInfo(
		IBindable owner,
		string name, 
		Action<Action<T?>> subscribeOwnerUpdated,
		ActionAsync<Func<T?, T?>> updateOwner)
	{
		_owner = owner;
		_name = name;
		_subscribeOwnerUpdated = subscribeOwnerUpdated;
		_update = updateOwner;
	}

	internal bool IsValid => _owner is not null;

	public void NotifyUpdated()
	{
		try
		{
			_owner.OnPropertyChanged(_name);
		}
		catch (Exception error)
		{
			_owner.Log().Error(
				$"Failed to notify property changed on for '{_owner.GetType().Name}.{_name}'.",
				error);
		}
	}

	public void Subscribe(Action<T?> onPropertyChanged)
		=> _subscribeOwnerUpdated(onPropertyChanged);

	public ValueTask Update(Func<T?, T?> updater, CancellationToken ct)
		=> _update(updater, ct);
}
