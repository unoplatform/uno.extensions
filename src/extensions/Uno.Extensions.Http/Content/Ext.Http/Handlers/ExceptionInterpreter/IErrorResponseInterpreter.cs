using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace Uno.Extensions.Http.Handlers
{
	public interface IErrorResponseInterpreter<TResponse>
	{
		/// <summary>
		/// Determines whether or not the response should be interpreted as an error.
		/// </summary>
		/// <param name="request"><see cref="HttpRequestMessage"/></param>
		/// <param name="response"><see cref="HttpResponseMessage"/></param>
		/// <param name="deserializedResponse"><typeparamref name="TResponse"/></param>
		/// <returns>True if the response is an error; false otherwise</returns>
		bool IsError(HttpRequestMessage request, HttpResponseMessage response, TResponse deserializedResponse);

		/// <summary>
		/// Gets the <see cref="Exception"/> that should be thrown for the specified <typeparamref name="TResponse"/>.
		/// </summary>
		/// <param name="request"><see cref="HttpRequestMessage"/></param>
		/// <param name="response"><see cref="HttpResponseMessage"/></param>
		/// <param name="deserializedResponse"><typeparamref name="TResponse"/></param>
		/// <returns><see cref="Exception"/></returns>
		Exception GetException(HttpRequestMessage request, HttpResponseMessage response, TResponse deserializedResponse);
	}
}
