using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Disposables;
using Uno.Equality;
using Uno.Extensions.Collections;
using Uno.Extensions.Reactive.Bindings.Collections;
using Uno.Extensions.Reactive.Utils;

namespace Umbrella.Presentation.Feeds.Tests.Collections._TestUtils
{
	internal class CompositeDisposable : IDisposable
	{
		private readonly List<IDisposable> _disposables;

		public CompositeDisposable(IEnumerable<IDisposable> disposables)
		{
			_disposables = disposables.ToList();
		}

		public CompositeDisposable(params IDisposable[] disposables)
		{
			_disposables = disposables.ToList();
		}

		public void Add(IDisposable disposable)
			=> _disposables.Add(disposable);

		/// <inheritdoc />
		public void Dispose()
		{
			foreach (var disposable in _disposables)
			{
				disposable.Dispose();
			}
		}
	}

	internal class ChangesRecorderBase<T>
	{
		private readonly IList<(object sender, string senderName, IVectorChangedEventArgs change, uint count, object[] snapshot)> _vectorChanges = new List<(object, string, IVectorChangedEventArgs, uint, object[])>();

		public ChangesRecorderBase(
			IObservableCollection<T> source, 
			IObservableVector<object> view)
		{
			Source = source;
			View = view;
		}

		public IDisposable RecordVectorChanges()
			=> RecordVectorChanges(0, View, "Root");

		protected virtual IDisposable RecordVectorChanges(int level, IObservableVector<object> view, string name, Func<IVectorChangedEventArgs, IDisposable>? onVectorChanged = null)
		{
			var subscriptions = new CompositeDisposable();

			//using (Context.AsCurrent())
			{
				view.VectorChanged += VectorChanged;
			}

			subscriptions.Add(Disposable
				.Create(() =>
				{
					//using (Context.AsCurrent())
					{
						view.VectorChanged -= VectorChanged;
					}
				}));

			return subscriptions;

			void VectorChanged(object snd, IVectorChangedEventArgs arg)
			{
				//using (Context.AsCurrent())
				{
					_vectorChanges.Add((snd, name, arg, (uint)view.Count, view.Select(o => o).ToArray()));
					var childSubscription = onVectorChanged?.Invoke(arg);
					if (childSubscription != null)
					{
						subscriptions.Add(childSubscription);
					}
				}
			}
		}

		public IObservableCollection<T> Source { get; }
		public IObservableVector<object> View { get; }

		public void ShouldBe(params IVectorChangeDescriptor[] expected)
		{
			//Schedulers.EndOfTime();

			//using (Context.AsCurrent())
			{
				Debug.WriteLine($"Expected: \r\n{string.Join("\r\n", expected.Select((e, i) => $"\t{i:D2}: {e.Change?.ToString() ?? "--"} @ {e.Index?.ToString() ?? "--"} in {e.SenderName} (Count: {e.CollectionCount?.ToString() ?? "--"})"))}");
				Debug.WriteLine($"Actuals: \r\n{string.Join("\r\n", _vectorChanges.Select((e, i) => $"\t{i:D2}: {e.change.CollectionChange} @ {e.change.Index} in {e.senderName} (Count: {e.count})"))}");

				Assert.AreEqual(expected.Length, _vectorChanges.Count, "Number of events mismatch");

				for (var i = 0; i < expected.Length; i++)
				{
					var capture = _vectorChanges[i];

					//Assert.AreSame(Collection, capture.sender, $"Change {i}: Sender is incorrect ({capture.sender.GetType().Name}).");
					expected[i].ShouldMatch(i, capture.change, capture.count, capture.snapshot);
				}
			}
		}
	}

	internal class LeafChangesRecorder<TItem> : ChangesRecorderBase<TItem>
	{
		public LeafChangesRecorder(
			IObservableCollection<TItem> source,
			BindableCollection view)
			: base(source, view)
		{
		}

		public void ShouldBe(params Func<VectorChangeDescriptor<TItem>, VectorChangeDescriptor<TItem>>[] expected)
			=> ShouldBe(expected.Select(e => e(VectorChangeDescriptor<TItem>.Empty)).ToArray());
	}

	internal class BranchChangesRecorder<TGroup, TItem> : ChangesRecorderBase<TGroup>
	{
		private readonly GroupedCollectionStructure<TItem> _structure;
		private readonly Dictionary<IObservableVector<object>, IDisposable> _groupsSubscriptions = new Dictionary<IObservableVector<object>, IDisposable>();

		public BranchChangesRecorder(
			IObservableCollection<TGroup> source,
			BindableCollection view)
			: base(source, view)
		{
			_structure = new GroupedCollectionStructure<TItem>(view);
		}

		protected override IDisposable RecordVectorChanges(int level, IObservableVector<object> view, string name, Func<IVectorChangedEventArgs, IDisposable>? onVectorChanged = null)
		{
			var subscription = base.RecordVectorChanges(level, view, name, onVectorChanged);

			//using (Context.AsCurrent())
			{
				if (view is ICollectionView c && c.CollectionGroups != null)
				{
					return new CompositeDisposable(
						subscription, 
						base.RecordVectorChanges(level, c.CollectionGroups, $"{name}.Groups", _ => SubscribeToGroups()),
						SubscribeToGroups());
				}
				else
				{
					return subscription;
				}

				IDisposable SubscribeToGroups()
				{
					var newSubscriptions = new List<IDisposable>();
					for (var i = 0; i < c.CollectionGroups.Count; i++)
					{
						var group = (ICollectionViewGroup)c.CollectionGroups[i];
						if (!_groupsSubscriptions.ContainsKey(@group.GroupItems))
						{
							newSubscriptions.Add(_groupsSubscriptions[@group.GroupItems] = RecordVectorChanges(level + 1, group.GroupItems, $"{name}.Groups[{i}]"));
						}
					}

					return newSubscriptions.Count > 0 ? new CompositeDisposable(newSubscriptions) : Disposable.Empty;
				}
			}
		}

		public void ShouldBe(params Func<GroupedCollectionStructure<TItem>, IVectorChangeDescriptor>[] expected)
			=> ShouldBe(expected.Select(e => e(_structure)).ToArray());

		public class GroupedCollectionStructure<T>
		{
			private readonly BindableCollection _view;

			public GroupedCollectionStructure(BindableCollection view)
			{
				_view = view;
			}

			public VectorChangeDescriptor<T> Root => new VectorChangeDescriptor<T>(_view, "Root");

			public VectorChangeDescriptor<object> Groups
			{
				get { /*using (_context.AsCurrent())*/ return new VectorChangeDescriptor<object>(_view.CollectionGroups, "Root.Groups");}
			}

			public VectorChangeDescriptor<T> this[int index]
			{
				get { /*using (_context.AsCurrent())*/ return new VectorChangeDescriptor<T>(_view.CollectionGroups[index], $"Root.Groups[{index}]"); }
			}
		}
	}
}
