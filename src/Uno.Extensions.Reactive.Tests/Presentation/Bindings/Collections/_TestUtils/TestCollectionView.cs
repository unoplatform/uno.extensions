using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.Foundation;

namespace Umbrella.Presentation.Feeds.Tests.Collections._TestUtils
{
	internal class TestCollectionView : List<object?>, ICollectionView
	{
		public event CurrentChangingEventHandler CurrentChanging { add => throw new NotImplementedException(); remove => throw new NotImplementedException(); }
		public event EventHandler<object?> CurrentChanged { add => throw new NotImplementedException(); remove => throw new NotImplementedException(); }
		public event NotifyCollectionChangedEventHandler CollectionChanged { add => throw new NotImplementedException(); remove => throw new NotImplementedException(); }
		public event VectorChangedEventHandler<object> VectorChanged { add => throw new NotImplementedException(); remove => throw new NotImplementedException(); }

		#region Grouping
		public IObservableVector<object?>? CollectionGroups { get; set; }
		#endregion

		#region Selection
		public object CurrentItem => throw new NotImplementedException();
		public int CurrentPosition => throw new NotImplementedException();
		public bool IsCurrentAfterLast => throw new NotImplementedException();
		public bool IsCurrentBeforeFirst => throw new NotImplementedException();
		public bool MoveCurrentTo(object item) => throw new NotImplementedException();
		public bool MoveCurrentToFirst() => throw new NotImplementedException();
		public bool MoveCurrentToLast() => throw new NotImplementedException();
		public bool MoveCurrentToNext() => throw new NotImplementedException();
		public bool MoveCurrentToPosition(int position) => throw new NotImplementedException();
		public bool MoveCurrentToPrevious() => throw new NotImplementedException();
		#endregion

		#region Pagination
		public bool HasMoreItems => throw new NotImplementedException();
		public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count) => throw new NotImplementedException();
		#endregion
	}
}
