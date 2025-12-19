using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// A factory of platform specific bindable friendly
/// </summary>
/// <remarks>
/// This interface is used to abstract the UI platform used to run the reactive framework.
/// It is not intended to be implemented by application.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)] // Should be used by UI module only, not apps
public interface IBindableFactory
{
	/// <summary>
	/// Creates a bindable friendly IListFeed from a given IListState
	/// </summary>
	/// <remarks>This gives the opportunity to create a platform specific bindable friendly version of collections.</remarks>
	/// <typeparam name="T">Type of the items of the collection.</typeparam>
	/// <param name="name">Name of the property backed by the resulting bindable list.</param>
	/// <param name="source">The source list state.</param>
	/// <returns>A bindable friendly list feed.</returns>
	IListFeed<T> CreateList<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
		T
	>(string name, IListState<T> source);
}
