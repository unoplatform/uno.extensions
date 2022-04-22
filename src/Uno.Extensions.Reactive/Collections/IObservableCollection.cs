using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Collections;

internal interface IObservableCollection : IList, INotifyCollectionChanged /*,INotifyPropertyChanged*/ /*TODO: Uno ,IExtensibleDisposable*/ /*TODO: Uno ,IDisposable */
{
	/// <summary>
	/// Atomatically adds a <see cref="INotifyCollectionChanged.CollectionChanged"/> event handler from any thread for a given scheduling context.
	/// <remarks>The handler is expected to be invoked on the provided scheduling context.</remarks>
	/// </summary>
	/// <param name="callback">The event handler.</param>
	/// <param name="current">The items when the handler was added.</param>
	/// <returns>
	/// An <see cref="IDisposable"/> which removes the handler when disposed.
	/// <remarks>Alternatively, you can use the <see cref="RemoveCollectionChangedHandler"/> method.</remarks>
	/// </returns>
	IDisposable AddCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current);

	/// <summary>
	/// Atomatically removes a <see cref="INotifyCollectionChanged.CollectionChanged"/> event handler from any thread for a given scheduling context.
	/// <remarks>
	/// If you don't need to retrieve the <paramref name="current"/> version of the collection, you should use the <see cref="IDisposable"/> returned by the <see cref="AddCollectionChangedHandler"/>.
	/// </remarks>
	/// </summary>
	/// <param name="callback">The event handler.</param>
	/// <param name="current">The items when the handler was added.</param>
	void RemoveCollectionChangedHandler(Action<RichNotifyCollectionChangedEventArgs> callback, out IObservableCollectionSnapshot current);

	#region Disambiguation
	new bool Remove(object item);
	#endregion
}
