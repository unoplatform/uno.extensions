using System;
using System.Linq;
using Uno.Extensions.Collections;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection
{
	internal interface IBindableCollectionViewSource
	{
		/// <summary>
		/// Gets the source of the parent data layer, if any.
		/// </summary>
		IBindableCollectionViewSource? Parent { get; }

		/* TODO Uno
		/// <summary>
		/// Gets the scheduler used by this view, usually the dispatcher (cf. Remarks)
		/// </summary>
		/// <remarks>
		/// You should have to use use this!
		/// Usually all the work is done on the UI thread and we should not re-dispatch some work if not necessary.
		/// (Bg work is usually using the `Schedule` method directly on the `DataLayerHolder` - the common implementation of this interface).
		/// </remarks>
		IScheduler Scheduler { get; }
		*/

		event EventHandler<CurrentSourceUpdateEventArgs> CurrentSourceChanging;

		event EventHandler<CurrentSourceUpdateEventArgs> CurrentSourceChanged;

		TFacet GetFacet<TFacet>();
	}

	internal class CurrentSourceUpdateEventArgs
	{
		public CurrentSourceUpdateEventArgs(IObservableCollection from, IObservableCollection to)
		{
			From = from;
			To = to;
		}

		public IObservableCollection From { get; }

		public IObservableCollection To { get; }
	}
}
