using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers
{
	public interface INetworkAvailabilityChecker
	{
		/// <summary>
		/// Checks if the network is currently available.
		/// </summary>
		/// <param name="ct"><see cref="CancellationToken"/></param>
		/// <returns>True if the network is available; false otherwise.</returns>
		Task<bool> CheckIsNetworkAvailable(CancellationToken ct);
	}
}
