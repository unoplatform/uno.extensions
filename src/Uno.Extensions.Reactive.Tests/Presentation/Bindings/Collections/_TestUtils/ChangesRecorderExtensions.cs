using System;
using System.Linq;
using Uno.Equality;
using Uno.Extensions.Collections;
using Uno.Extensions.Reactive.Bindings.Collections;

namespace Umbrella.Presentation.Feeds.Tests.Collections._TestUtils
{
	internal static class ChangesRecorderExtensions
	{
		public static LeafChangesRecorder<T> Do<T>(this LeafChangesRecorder<T> recorder, params Action<IObservableCollection<T>>[] operations)
		{
			recorder.Schedulers.Advance();

			using (recorder.Schedulers.Background().AsCurrent())
			{
				foreach (var operation in operations)
				{
					operation(recorder.Source);
					recorder.Schedulers.Advance();
				}
			}

			return recorder;
		}

		public static BranchChangesRecorder<TGroup, TItem> Do<TGroup, TItem>(this BranchChangesRecorder<TGroup, TItem> recorder, params Action<IObservableCollection<TGroup>>[] operations)
		{
			recorder.Schedulers.Advance();

			using (recorder.Schedulers.Background().AsCurrent())
			{
				foreach (var operation in operations)
				{
					operation(recorder.Source);
					recorder.Schedulers.Advance();
				}
			}

			return recorder;
		}

		public static LeafChangesRecorder<T> Switch<T>(this LeafChangesRecorder<T> recorder, IObservableCollection<T> newCollection, TrackingMode mode = TrackingMode.Auto)
		{
			recorder.Schedulers.Advance();

			using (recorder.Schedulers.Background().AsCurrent())
			{
				((BindableCollection)recorder.View).Switch(newCollection, mode);
			}

			return recorder;
		}

		public static BranchChangesRecorder<TGroup, TItem> Switch<TGroup, TItem>(this BranchChangesRecorder<TGroup, TItem> recorder, IObservableCollection<TGroup> newCollection, TrackingMode mode = TrackingMode.Auto)
		{
			recorder.Schedulers.Advance();

			using (recorder.Schedulers.Background().AsCurrent())
			{
				((BindableCollection)recorder.View).Switch(newCollection, mode);
			}

			return recorder;
		}

	}
}
