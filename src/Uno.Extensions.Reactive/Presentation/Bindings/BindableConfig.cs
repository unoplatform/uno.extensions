using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// Advanced configuration flags for <see cref="Bindable{T}"/>.
/// </summary>
[Flags]
internal enum BindableConfig
{
	/// <summary>
	/// Automatically initialize the Bindable (Cf. remarks for details).
	/// </summary>
	/// <remarks>
	/// During the initialization, the bindable subscribes to the given property in order to be notified when the value changed.
	/// But the "updated callback" is also expected to be invoked synchronously in order to get the current value (if ready).
	/// If so, the current value will be applied locally (`SetValueCore`) which drives to invoke the `UpdateSubProperties` which is a virtual method.
	/// Inheritors can remove that flag to avoid this issue. Is so, it's their responsibility to invoke the `Initialize` method at the end of their constructor.
	/// </remarks>
	AutoInit = 1,

	/// <summary>
	/// Indicates that the inheritor has a property name `Value` which can be data bind directly instead of <see cref="Bindable{T}.GetValue"/> and <see cref="Bindable{T}.SetValue(T)"/>.
	/// Is so, the <see cref="INotifyPropertyChanged.PropertyChanged"/> will be raise accordingly.
	/// </summary>
	RaiseValuePropertyChanged,

	Default = AutoInit
}
