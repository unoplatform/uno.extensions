using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers
{
	public interface IResponseContentDeserializer
	{
		/// <summary>
		/// Deserializes the response into the 
		/// </summary>
		/// <typeparam name="TResponse"></typeparam>
		/// <param name="ct"></param>
		/// <param name="content"></param>
		/// <returns></returns>
		Task<TResponse> Deserialize<TResponse>(CancellationToken ct, HttpContent content);
	}
}
