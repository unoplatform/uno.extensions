using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Http;

namespace Uno.Extensions.Aspire.Tests;

[TestClass]
public class Given_DiagnosticHandler
{
	[TestMethod]
	public async Task When_RequestCarriesCredentials_Then_TheirValuesAreNotLogged()
	{
		var logger = new RecordingLogger<DiagnosticHandler>();
		using var invoker = new HttpMessageInvoker(new DiagnosticHandler(logger) { InnerHandler = new StubHttpHandler(HttpStatusCode.OK) });
		using var request = new HttpRequestMessage(HttpMethod.Get, "http://api.test/items?sig=query-secret");
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "client-secret");
		request.Headers.TryAddWithoutValidation("Cookie", "session=client-cookie");
		request.Headers.TryAddWithoutValidation("Proxy-Authorization", "Basic proxy-secret");
		request.Headers.TryAddWithoutValidation("X-Api-Key", "api-key-secret");
		request.Headers.TryAddWithoutValidation("X-Trace", "visible-value");

		await invoker.SendAsync(request, CancellationToken.None);

		var log = string.Join("\n", logger.Messages);
		log.Should().NotContain("client-secret").And.NotContain("client-cookie").And.NotContain("server-secret");
		log.Should().NotContain("proxy-secret").And.NotContain("api-key-secret").And.NotContain("query-secret");
		log.Should().Contain("Authorization: [redacted]").And.Contain("Set-Cookie: [redacted]").And.Contain("X-Api-Key: [redacted]");
		log.Should().Contain("GET /items ");
		log.Should().Contain("X-Trace: visible-value");
	}
}
