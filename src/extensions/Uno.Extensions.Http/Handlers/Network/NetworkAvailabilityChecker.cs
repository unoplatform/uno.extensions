using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers
{
    public class NetworkAvailabilityChecker : INetworkAvailabilityChecker
    {
        public Task<bool> CheckIsNetworkAvailable(CancellationToken ct)
        {
#if WINDOWS_UWP || __ANDROID__ || __IOS__
            // TODO #172362: Not implemented in Uno.
            // return NetworkInformation.GetInternetConnectionProfile()?.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
            return Task.FromResult(Xamarin.Essentials.Connectivity.NetworkAccess == Xamarin.Essentials.NetworkAccess.Internet);
#else
            return Task.FromResult(true);
#endif
        }
    }
}
