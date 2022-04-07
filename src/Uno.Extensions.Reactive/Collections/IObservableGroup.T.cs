using System;
using System.Linq;

namespace nVentive.Umbrella.Collections
{
	/// <summary>
	/// A group of <typeparamref name="T"/> which notifies read and write oprations.
	/// </summary>
	/// <typeparam name="T">Type of the items</typeparam>
	public interface IObservableGroup<T> : IObservableGroup, IObservableCollection<T>
	{
	}
}
