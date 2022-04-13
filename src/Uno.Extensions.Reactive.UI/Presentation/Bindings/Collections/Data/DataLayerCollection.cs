using System;
using System.Linq;
using System.Runtime.CompilerServices;
using nVentive.Umbrella.Collections;
using nVentive.Umbrella.Conversion;

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection.Data
{
	/// <summary>
	/// A collection responsible to maintain the holders for the children items of a branch 
	/// (which can be either some other branches or some leafs)
	/// </summary>
	internal class DataLayerCollection : IConverter<IObservableGroup, BindableCollectionGroup>
	{
		private readonly ConditionalWeakTable<IObservableGroup, BindableCollectionGroup> _views = new();

		#region IConverter<IObservableGroup, BindableCollectionGroup>
		BindableCollectionGroup IConverter<IObservableGroup, BindableCollectionGroup>.Convert(IObservableGroup model)
			=> Get(model);

		IObservableGroup IConverter<IObservableGroup, BindableCollectionGroup>.ConvertBack(BindableCollectionGroup to) 
			=> (IObservableGroup)to.Group;
		#endregion

		public void Add(IObservableGroup model, BindableCollectionGroup view)
		{
			_views.Remove(model); // If a group was added and removed, collection tracker may detect it as a Add. We have to cleanup the previous value.
			_views.Add(model, view);
		}

		public BindableCollectionGroup? Find(IObservableGroup model)
			=> _views.TryGetValue(model, out var view) ? view : default;

		public BindableCollectionGroup Get(IObservableGroup model) 
			=> _views.TryGetValue(model, out var view) ? view : throw new InvalidOperationException("Group does not exist");

		public BindableCollectionGroup Update(IObservableGroup oldGroup, IObservableGroup newGroup)
		{
			if (!_views.TryGetValue(oldGroup, out var view))
			{
				throw new InvalidOperationException("Group did not exist");
			}

			if (!object.ReferenceEquals(oldGroup, newGroup))
			{
				_views.Remove(newGroup);
				_views.Add(newGroup, view);
			}

			return view;
		}

		public BindableCollectionGroup Remove(IObservableGroup model)
		{
			if (!_views.TryGetValue(model, out var view))
			{
				throw new InvalidOperationException("Group did not exist");
			}

			return view;
		}
	}
}
