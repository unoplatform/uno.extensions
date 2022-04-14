using System;
using System.Collections.Specialized;
using System.Linq;
using Uno.Extensions.Collections;

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection.Data
{
	/// <summary>
	/// A counter that can be used to configure the tracking behavior
	/// </summary>
	internal interface IUpdateContext
	{
		/// <summary>
		/// Gets the type of update
		/// </summary>
		VisitorType Type { get; }

		/// <summary>
		/// Gets the mode use to track collection changes for this update
		/// </summary>
		TrackingMode Mode { get; }

		/// <summary>
		/// Indicates that the number of changes is now to high, and a <see cref="NotifyCollectionChangedAction.Reset"/> should be raised instead of a properly tracking the changes.
		/// </summary>
		bool HasReachedLimit { get; }

		/// <summary>
		/// Increase the add counter
		/// </summary>
		void NotifyAdd();

		/// <summary>
		/// Increase the same item counter
		/// </summary>
		void NotifySameItem();

		/// <summary>
		/// Increase the replace counter
		/// </summary>
		void NotifyReplace();

		/// <summary>
		/// Increase the remove counter
		/// </summary>
		void NotifyRemove();

		/// <summary>
		/// Increase the reset counter
		/// </summary>
		void NotifyReset();
	}
}
