namespace Uno.Extensions.Http;

/// <summary>
/// Handler for printing diagnostic information about requests
/// </summary>
public class DiagnosticHandler : DelegatingHandler
{
	private const string RedactedValue = "[redacted]";

	private readonly ILogger _logger;

	/// <summary>
	/// Creates a new instance of <see cref="DiagnosticHandler"/>
	/// </summary>
	/// <param name="logger">ILogger for logging</param>
	public DiagnosticHandler(ILogger<DiagnosticHandler> logger)
	{
		_logger = logger;
	}

	/// <inheritdoc/>
	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		if (!_logger.IsEnabled(LogLevel.Information))
		{
			return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
		}

		var req = request;
		try
		{
			_logger.LogInformationMessage($"Host: {req.RequestUri?.Scheme}://{req.RequestUri?.Host}");
			// Security: path only, a query string can carry a token.
			_logger.LogInformationMessage($"Method: {req.Method} {req.RequestUri?.AbsolutePath} {req.RequestUri?.Scheme}/{req.Version}");

			foreach (var header in req.Headers)
			{
				_logger.LogInformationMessage($"Header: {header.Key}: {LoggableValue(header)}");
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
				$"Response: {req.RequestUri?.Scheme.ToUpper()}/{resp.Version} {(int)resp.StatusCode} {resp.ReasonPhrase}");

			foreach (var header in resp.Headers)
			{
				_logger.LogInformationMessage($"Header (response): {header.Key}: {LoggableValue(header)}");
			}

		}
		catch (Exception ex)
		{
			_logger.LogInformationMessage($"Error logging response - {ex.Message}");
		}

		return response;
	}

	// Security: credentials must never reach a log sink.
	private static string LoggableValue(KeyValuePair<string, IEnumerable<string>> header) =>
		IsCredentialHeader(header.Key) ? RedactedValue : string.Join(", ", header.Value);

	private static readonly string[] CredentialNameParts = ["auth", "cookie", "key", "token", "secret"];

	private static bool IsCredentialHeader(string name)
	{
		foreach (var part in CredentialNameParts)
		{
			if (name.Contains(part, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}
}
