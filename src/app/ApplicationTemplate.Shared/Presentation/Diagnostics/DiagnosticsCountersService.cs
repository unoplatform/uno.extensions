using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ApplicationTemplate.Presentation
{
	public class DiagnosticsCountersService
	{
		public event EventHandler CountersChanged;

		public CountersData Counters { get; private set; }

		public void Start()
		{
			Counters = new CountersData();

			DiagnosticListener.AllListeners.Subscribe(delegate(DiagnosticListener listener)
			{
				if (listener.Name.StartsWith("Chinook.DynamicMvvm.IViewModel", StringComparison.InvariantCulture))
				{
					listener.Subscribe(OnViewModelNotification);
				}
				else if (listener.Name.StartsWith("Chinook.DynamicMvvm.IDynamicProperty", StringComparison.InvariantCulture))
				{
					listener.Subscribe(OnPropertyNotification);
				}
				else if (listener.Name.StartsWith("Chinook.DynamicMvvm.IDynamicCommand", StringComparison.InvariantCulture))
				{
					listener.Subscribe(OnCommandNotification);
				}
			});
		}

		private void OnViewModelNotification(KeyValuePair<string, object> notification)
		{
			switch (notification.Key)
			{
				case "Created":
					Counters = Counters.IncrementCreatedViewModels();
					break;

				case "Disposed":
					Counters = Counters.IncrementDisposedViewModels();
					break;

				case "Destroyed":
					Counters = Counters.IncrementDestroyedViewModels();
					break;
			}

			CountersChanged?.Invoke(this, EventArgs.Empty);
		}

		private void OnPropertyNotification(KeyValuePair<string, object> notification)
		{
			switch (notification.Key)
			{
				case "Created":
					Counters = Counters.IncrementCreatedProperties();
					break;

				case "Disposed":
					Counters = Counters.IncrementDisposedProperties();
					break;

				case "Destroyed":
					Counters = Counters.IncrementDestroyedProperties();
					break;
			}

			CountersChanged?.Invoke(this, EventArgs.Empty);
		}

		private void OnCommandNotification(KeyValuePair<string, object> notification)
		{
			switch (notification.Key)
			{
				case "Created":
					Counters = Counters.IncrementCreatedCommands();
					break;

				case "Disposed":
					Counters = Counters.IncrementDisposedCommands();
					break;

				case "Destroyed":
					Counters = Counters.IncrementDestroyedCommands();
					break;
			}

			CountersChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	public partial class CountersData
	{
		public CountersData()
		{
		}

		public CountersData(CountersData source)
		{
			if (source is null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			CreatedViewModels = source.CreatedViewModels;
			DisposedViewModels = source.DisposedViewModels;
			DestroyedViewModels = source.DestroyedViewModels;

			CreatedProperties = source.CreatedProperties;
			DisposedProperties = source.DisposedProperties;
			DestroyedProperties = source.DestroyedProperties;

			CreatedCommands = source.CreatedCommands;
			DisposedCommands = source.DisposedCommands;
			DestroyedCommands = source.DestroyedCommands;
		}

		public int CreatedViewModels { get; private set; }

		public int DisposedViewModels { get; private set; }

		public int DestroyedViewModels { get; private set; }

		public int CreatedProperties { get; private set; }

		public int DisposedProperties { get; private set; }

		public int DestroyedProperties { get; private set; }

		public int CreatedCommands { get; private set; }

		public int DisposedCommands { get; private set; }

		public int DestroyedCommands { get; private set; }

		/// <summary>
		/// Gets the number of view models created but not disposed.
		/// </summary>
		public int ActiveViewModels => CreatedViewModels - DisposedViewModels;

		/// <summary>
		/// Gets the number of view models disposed but not destroyed.
		/// </summary>
		public int UncollectedViewModels => DisposedViewModels - DestroyedViewModels;

		/// <summary>
		/// Gets the number of properties created but not disposed.
		/// </summary>
		public int ActiveProperties => CreatedProperties - DisposedProperties;

		/// <summary>
		/// Gets the number of properties disposed but not destroyed.
		/// </summary>
		public int UncollectedProperties => DisposedProperties - DestroyedProperties;

		/// <summary>
		/// Gets the number of commands created but not disposed.
		/// </summary>
		public int ActiveCommands => CreatedCommands - DisposedCommands;

		/// <summary>
		/// Gets the number of commands disposed but not destroyed.
		/// </summary>
		public int UncollectedCommands => DisposedCommands - DestroyedCommands;

		#region Increments
		public CountersData IncrementCreatedViewModels()
			=> new CountersData(this) { CreatedViewModels = CreatedViewModels + 1 };

		public CountersData IncrementDisposedViewModels()
			=> new CountersData(this) { DisposedViewModels = DisposedViewModels + 1 };

		public CountersData IncrementDestroyedViewModels()
			=> new CountersData(this) { DestroyedViewModels = DestroyedViewModels + 1 };

		public CountersData IncrementCreatedProperties()
			=> new CountersData(this) { CreatedProperties = CreatedProperties + 1 };

		public CountersData IncrementDisposedProperties()
			=> new CountersData(this) { DisposedProperties = DisposedProperties + 1 };

		public CountersData IncrementDestroyedProperties()
			=> new CountersData(this) { DestroyedProperties = DestroyedProperties + 1 };

		public CountersData IncrementCreatedCommands()
			=> new CountersData(this) { CreatedCommands = CreatedCommands + 1 };

		public CountersData IncrementDisposedCommands()
			=> new CountersData(this) { DisposedCommands = DisposedCommands + 1 };

		public CountersData IncrementDestroyedCommands()
			=> new CountersData(this) { DestroyedCommands = DestroyedCommands + 1 };
		#endregion
	}
}
