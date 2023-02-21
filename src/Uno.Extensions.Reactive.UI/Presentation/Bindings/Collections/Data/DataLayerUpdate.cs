using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions.Collections;
using Uno.Extensions.Collections.Facades.Differential;
using Uno.Extensions.Collections.Tracking;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data;
using Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Facets;

namespace Uno.Extensions.Reactive.Bindings.Collections._BindableCollection.Data
{
	internal class DataLayerUpdate : ICompositeCallback
	{
		private readonly List<BeforeCallback> _before = new();
		private readonly List<AfterCallback> _after = new();

		private readonly IObservableCollectionSnapshot? _from;
		private readonly CollectionUpdater _changes;
		private readonly CollectionFacet _target;
		private readonly IUpdateContext _context;
		private readonly DataLayerChangesBuffer _buffer;

		/// <summary>
		/// A snapshot of the items once this initializer has been completed
		/// </summary>
		public IObservableCollectionSnapshot Result { get; }

		public DataLayerUpdate(
			IObservableCollectionSnapshot? from,
			CollectionUpdater changes, 
			IObservableCollectionSnapshot to, 
			CollectionFacet target,
			IUpdateContext context,
			DataLayerChangesBuffer buffer)
		{
			_from = from;
			_changes = changes;
			Result = to;
			_target = target;
			_context = context;
			_buffer = buffer;
		}

		/// <summary>
		/// Prepend a callback that has to be executed before the completion of this initializer
		/// </summary>
		public void Prepend(BeforeCallback before) => _before.Add(before);

		/// <summary>
		/// Prepend a callback that has to be executed after the completion of this initializer
		/// </summary>
		public void Append(AfterCallback after) => _after.Add(after);

		/// <summary>
		/// Completes the initialization of this data layer (for a root layer)
		/// </summary>
		public void Complete()
		{
			// This will be invoked only when this data layer is a root layer

			// If this update has been created from a background thread (init),
			// it might happen that we already have some event handlers, 
			// so we must make sure to raise event properly (i.e. silently: false)
			Invoke(CallbackPhase.All, silently: false);

			_buffer.Start();
		}

		// This will be invoked only when this data layer is a child layer
		void ICompositeCallback.Invoke(CallbackPhase phases, bool silently)
			=> Invoke(phases, silently);

		private void Invoke(CallbackPhase phases, bool silently)
		{
			if (phases.HasFlag(CallbackPhase.Before))
			{
				_before.InvokeAll();
			}

			if (phases.HasFlag(CallbackPhase.Main))
			{
				if (silently)
				{
					_target.UpdateSilentlyTo(_changes, Result);
				}
				else if (_context.HasReachedLimit)
				{
					// We should reach this code only for a root holder, for children we should have been invoked with 'silently = true'
					_target.UpdateTo(_changes.ToReset(_target.Head.AsList(), Result), Result);
				}
				else
				{
					_target.UpdateTo(_changes, Result);
				}
			}

			if (phases.HasFlag(CallbackPhase.After))
			{
				_after.InvokeAll();
			}
		}

		public AfterCallback ParentAfter => () => _buffer.Start();
	}
}
