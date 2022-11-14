using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// Flags an object as a _model_.
/// </summary>
/// <typeparam name="TViewModel"></typeparam>
/// <remarks>
/// This interface is expected to be implemented by the code gen, you should not have to implement it in your code.
/// EVen if it's not recommended, this gives to the _Model_ to ability to interact with its _ViewModel_ if needed.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public interface IModel<out TViewModel>
{
	/// <summary>
	/// Gets the instance of the view model associated to this model.
	/// </summary>
	TViewModel ViewModel { get; }
}
