using System;
using System.Collections.Generic;
using System.Text;
using Chinook.DataLoader;
using MallardMessageHandlers;

namespace ApplicationTemplate
{
//-:cnd:noEmit
#if __ANDROID__ || __IOS__ || WINDOWS_UWP
//+:cnd:noEmit
	/// <summary>
	/// A <see cref="IDataLoaderTrigger"/> that will request a load
	/// when the <see cref="IDataLoader"/> is in a no network state
	/// and that the network connectivity is regained.
	/// </summary>
	public sealed class NetworkReconnectionDataLoaderTrigger : DataLoaderTriggerBase
	{
		private readonly IDataLoader _dataLoader;

		public NetworkReconnectionDataLoaderTrigger(IDataLoader dataLoader)
			: base("NetworkReconnection")
		{
			_dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));

			Xamarin.Essentials.Connectivity.ConnectivityChanged += OnConnectivityChanged;
		}

		private void OnConnectivityChanged(object sender, Xamarin.Essentials.ConnectivityChangedEventArgs e)
		{
			// Should only refresh when loader is in NoNetwork AND network is now active
			if (_dataLoader.State.Error is NoNetworkException &&
				e.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet)
			{
				RaiseLoadRequested();
			}
		}

		public override void Dispose()
		{
			base.Dispose();

			Xamarin.Essentials.Connectivity.ConnectivityChanged -= OnConnectivityChanged;
		}
	}
	//-:cnd:noEmit
#else
//+:cnd:noEmit
	/// <summary>
	/// Not implemented
	/// </summary>
	public sealed class NetworkReconnectionDataLoaderTrigger : DataLoaderTriggerBase
	{
		public NetworkReconnectionDataLoaderTrigger(IDataLoader dataLoader)
			: base("NetworkReconnection")
		{
		}

		public override void Dispose()
		{
			base.Dispose();
		}
	}
//-:cnd:noEmit
#endif
	//+:cnd:noEmit

	public static class NetworkReconnectionDataLoaderExtensions
	{
		public static TBuilder TriggerOnNetworkReconnection<TBuilder>(this TBuilder dataLoaderBuilder)
			where TBuilder : IDataLoaderBuilder
			=> (TBuilder)dataLoaderBuilder.WithTrigger(d => new NetworkReconnectionDataLoaderTrigger(d));
	}
}
