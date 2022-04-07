#if !HAS_WINDOWS_UI && !HAS_UMBRELLA_UI && false
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;

namespace Umbrella.Presentation.Feeds.Collections
{
	partial class BindableCollection : ICollectionView
	{
		public void Refresh() => throw new NotImplementedException();

		public IDisposable DeferRefresh() => throw new NotImplementedException();

		public CultureInfo Culture { get; set; }
		public IEnumerable SourceCollection => throw new NotImplementedException();
		public Predicate<object> Filter { get; set; }
		public bool CanFilter { get; }
		public System.ComponentModel.SortDescriptionCollection SortDescriptions { get; }
		public bool CanSort { get; }
		public bool CanGroup { get; }
		public ObservableCollection<System.ComponentModel.GroupDescription> GroupDescriptions { get; }
		public ReadOnlyObservableCollection<object> Groups { get; }
		public bool IsEmpty { get; }
	}
}

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection.Views
{
	partial class BasicView : ICollectionView
	{
		public void Refresh() => throw new NotImplementedException();
		public IDisposable DeferRefresh() => throw new NotImplementedException();
		public CultureInfo Culture { get; set; }
		public IEnumerable SourceCollection => throw new NotImplementedException();
		public Predicate<object> Filter { get; set; }
		public bool CanFilter { get; }
		public System.ComponentModel.SortDescriptionCollection SortDescriptions { get; }
		public bool CanSort { get; }
		public bool CanGroup { get; }
		public ObservableCollection<System.ComponentModel.GroupDescription> GroupDescriptions { get; }
		public ReadOnlyObservableCollection<object> Groups { get; }
		public bool IsEmpty { get; }
	}

	partial class FlatView : ICollectionView
	{
		public void Refresh() => throw new NotImplementedException();
		public IDisposable DeferRefresh() => throw new NotImplementedException();
		public CultureInfo? Culture { get; set; }
		public IEnumerable SourceCollection => throw new NotImplementedException();
		public Predicate<object> Filter { get; set; }
		public bool CanFilter { get; }
		public System.ComponentModel.SortDescriptionCollection SortDescriptions { get; }
		public bool CanSort { get; }
		public bool CanGroup { get; }
		public ObservableCollection<System.ComponentModel.GroupDescription> GroupDescriptions { get; }
		public ReadOnlyObservableCollection<object?>? Groups { get; }
		public bool IsEmpty { get; }
	}
}
#endif
