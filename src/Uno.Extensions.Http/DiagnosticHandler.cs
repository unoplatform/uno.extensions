using Uno.Extensions.Logging;

namespace Uno.Extensions.Http;

public class DiagnosticHandler : DelegatingHandler
{
	private readonly ILogger _logger;

	public DiagnosticHandler(ILogger<DiagnosticHandler> logger)
	{
		_logger = logger;
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var req = request;
		try
		{
			_logger.LogInformationMessage($"Host: {req.RequestUri.Scheme}://{req.RequestUri.Host}");
			_logger.LogInformationMessage($"Method: {req.Method} {req.RequestUri.PathAndQuery} {req.RequestUri.Scheme}/{req.Version}");

			foreach (var header in req.Headers)
			{
				_logger.LogInformationMessage($"Header: {header.Key}: {string.Join(", ", header.Value)}");
			}
		}
		catch (Exception ex)
		{
			_logger.LogInformationMessage($"Error logging request - {ex.Message}");
		}

		var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

		try
		{
			var resp = response;
			_logger.LogInformationMessage(
				$"Response: {req.RequestUri.Scheme.ToUpper()}/{resp.Version} {(int)resp.StatusCode} {resp.ReasonPhrase}");

			foreach (var header in resp.Headers)
			{
				_logger.LogInformationMessage($"Header (response): {header.Key}: {string.Join(", ", header.Value)}");
			}

		}
		catch (Exception ex)
		{
			_logger.LogInformationMessage($"Error logging response - {ex.Message}");
		}

		return response;
	}
}
