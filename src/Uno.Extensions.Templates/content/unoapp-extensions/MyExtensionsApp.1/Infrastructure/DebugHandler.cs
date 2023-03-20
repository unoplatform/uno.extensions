//+:cnd:noEmit
namespace MyExtensionsApp._1.Infrastructure;

internal class DebugHttpHandler : DelegatingHandler
{
#if (useLogging)
	private readonly ILogger _logger;
	public DebugHttpHandler(ILogger<DebugHttpHandler> logger, HttpMessageHandler? innerHandler = null)
		: base(innerHandler ?? new HttpClientHandler())
	{
		_logger = logger;
	}
#else
	public DebugHttpHandler(HttpMessageHandler? innerHandler = null)
		: base(innerHandler ?? new HttpClientHandler())
	{
	}
#endif

	protected async override Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var response = await base.SendAsync(request, cancellationToken);
//-:cnd:noEmit
#if DEBUG
//+:cnd:noEmit
		if(!response.IsSuccessStatusCode)
		{
#if (useLogging)
			_logger.LogDebugMessage("Unsuccessful API Call");
			if(request.RequestUri is not null)
				_logger.LogDebugMessage($"{request.RequestUri} ({request.Method})");
			foreach((var key, var values) in request.Headers.ToDictionary(x => x.Key, x => string.Join(", ", x.Value)))
			{
				_logger.LogDebugMessage($"{key}: {values}");
			}

			var content = request.Content is not null ? await request.Content.ReadAsStringAsync() : null;
			if(!string.IsNullOrEmpty(content))
			{
				_logger.LogDebugMessage(content);
			}
#else
			Console.Error.WriteLine("Unsuccessful API Call");
			if(request.RequestUri is not null)
				Console.Error.WriteLine($"{request.RequestUri} ({request.Method})");
			foreach((var key, var values) in request.Headers.ToDictionary(x => x.Key, x => string.Join(", ", x.Value)))
			{
				Console.Error.WriteLine($"  {key}: {values}");
			}

			var content = request.Content is not null ? await request.Content.ReadAsStringAsync() : null;
			if(!string.IsNullOrEmpty(content))
			{
				Console.Error.WriteLine(content);
			}
#endif

			// Uncomment to automatically break when an API call fails while debugging
			// System.Diagnostics.Debugger.Break();
		}
//-:cnd:noEmit
#endif
		return response;
	}
}
