using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Bindings;

/// <summary>
/// Helpers to create bindable friendly properties.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)] // Should be used by code gen only
public class BindableHelper
{
	private static IBindableFactory _factory = new NullBindableFactory();

	/// <summary>
	/// Configures the factory to use for create bindable
	/// </summary>
	/// <param name="factory"></param>
	[EditorBrowsable(EditorBrowsableState.Never)] // Should be used by module init only
	public static void ConfigureFactory(IBindableFactory factory)
		=> _factory = factory;

	/// <summary>
	/// Creates a bindable friendly IListFeed from a given IListState
	/// </summary>
	/// <remarks>This gives the opportunity to create a platform specific bindable friendly version of collections.</remarks>
	/// <typeparam name="T">Type of the items of the collection.</typeparam>
	/// <param name="name">Name of the property backed by the resulting bindable list.</param>
	/// <param name="source">The source list state.</param>
	/// <returns>A bindable friendly list feed.</returns>
	public static IListFeed<T> CreateBindableList<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
		T
	>(string name, IListState<T> source)
		=> _factory.CreateList(name, source);

	private class NullBindableFactory : IBindableFactory
	{
		/// <inheritdoc />
		public IListFeed<T> CreateList<
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
			T
		>(string name, IListState<T> source)
			=> source;
	}
}
