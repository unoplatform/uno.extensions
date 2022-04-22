using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Foundation.Collections;
using Uno.Extensions.Collections;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection
{
	/// <summary>
	/// A group of items of a bindable collection
	/// </summary>
	internal class BindableCollectionGroup : ICollectionViewGroup, INotifyPropertyChanged
	{
		private object _group;

		/// <inheritdoc />
		public event PropertyChangedEventHandler? PropertyChanged;

		public BindableCollectionGroup(IObservableGroup group, DataLayer layer)
		{
			_group = group;
			Holder = layer;
		}

		public void UpdateGroup(IObservableGroup newInstance)
		{
			_group = newInstance;
		}

		/// <summary>
		/// Gets the holder responsible to maintain the current version of the source
		/// </summary>
		public DataLayer Holder { get; }

		/// <inheritdoc />
		public object Group
		{
			get => _group;
			private set
			{
				_group = value;
				OnPropertyChanged();
			}
		}

		/// <inheritdoc />
		public IObservableVector<object> GroupItems => Holder.View;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) 
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
