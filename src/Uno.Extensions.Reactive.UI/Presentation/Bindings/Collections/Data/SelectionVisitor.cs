using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions.Collections.Tracking;
using Umbrella.Presentation.Feeds.Collections._BindableCollection.Facets;

namespace Umbrella.Presentation.Feeds.Collections._BindableCollection.Data
{
	internal class SelectionVisitor : CollectionUpdaterVisitor
	{
		private readonly SelectionFacet _selectionFacet;

		public SelectionVisitor(SelectionFacet selectionFacet)
		{
			_selectionFacet = selectionFacet;
		}

		/// <summary>
		/// Ensure the current selection stays the same after a Replace by reapplying it if it has been changed by the UI. 
		/// </summary>
		public override bool ReplaceItem(object original, object updated, ICollectionUpdateCallbacks callbacks)
		{
			int selectionBefore = -1;

			callbacks.Prepend(BeforeReplaceUI);
			callbacks.Append(AfterReplaceUI);

			return false;

			void BeforeReplaceUI()
			{
				selectionBefore = _selectionFacet.CurrentPosition;
			}

			void AfterReplaceUI()
			{
				var selectionAfter = _selectionFacet.CurrentPosition;
				if (selectionBefore > -1 && selectionAfter < 0)
				{
					_selectionFacet.MoveCurrentToPosition(selectionBefore);
				}
			}
		}
	}
}
