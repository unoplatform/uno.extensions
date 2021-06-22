using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers
{
	public class NetworkAvailabilityChecker : INetworkAvailabilityChecker
	{
		private readonly Func<CancellationToken, Task<bool>> _checkFunction;

		public NetworkAvailabilityChecker(Func<CancellationToken, Task<bool>> checkFunction)
		{
			_checkFunction = checkFunction ?? throw new ArgumentNullException(nameof(checkFunction));
		}

		public Task<bool> CheckIsNetworkAvailable(CancellationToken ct)
		{
			return _checkFunction(ct);
		}
	}
}
