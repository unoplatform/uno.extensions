using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Http.Handlers
{
	/// <summary>
	/// This <see cref="HttpMessageHandler"/> deserializes the response
	/// to a specific type <typeparamref name="TResponse"/>. If the response is
	/// interpreted as an error, it throws an exception.
	/// </summary>
	/// <typeparam name="TResponse">Type of response</typeparam>
	
	public class ExceptionInterpreterHandler<TResponse> : ExceptionInterpreterHandlerBase
	{
		private readonly IErrorResponseInterpreter<TResponse> _interpreter;
		private readonly IResponseContentDeserializer _responseDeserializer;

		/// <summary>
		/// Initializes a new instance of the <see cref="ExceptionInterpreterHandler{TResponse}"/> class.
		/// </summary>
		/// <param name="interpreter"><see cref="IErrorResponseInterpreter{TResponse}"/></param>
		/// <param name="responseDeserializer"><see cref="IResponseContentDeserializer"/></param>
		public ExceptionInterpreterHandler(
			IErrorResponseInterpreter<TResponse> interpreter,
			IResponseContentDeserializer responseDeserializer
		)
		{
			_interpreter = interpreter ?? throw new ArgumentNullException(nameof(interpreter));
			_responseDeserializer = responseDeserializer ?? throw new ArgumentNullException(nameof(responseDeserializer));
		}

		/// <inheritdoc/>
		protected override async Task<Exception> InterpretException(
			CancellationToken ct,
			HttpRequestMessage request,
			HttpResponseMessage response
		)
		{
			var deserializedResponse = await _responseDeserializer.Deserialize<TResponse>(ct, response.Content);

			return _interpreter.IsError(request, response, deserializedResponse)
				? _interpreter.GetException(request, response, deserializedResponse)
				: null;
		}
	}
}
