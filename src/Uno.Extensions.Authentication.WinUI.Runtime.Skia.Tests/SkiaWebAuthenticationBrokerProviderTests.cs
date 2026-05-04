#nullable enable
using System.Net;
using System.Net.Http;
using System.Web;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Authentication;
using Windows.Security.Authentication.Web;

namespace Uno.Extensions.Authentication.Tests;

[TestClass]
public class SkiaWebAuthenticationBrokerProviderTests
{
	[TestMethod]
	public void GetCurrentApplicationCallbackUri_ReturnsLoopbackWithPort()
	{
		var provider = new SkiaWebAuthenticationBrokerProvider();

		var uri = provider.GetCurrentApplicationCallbackUri();

		uri.Scheme.Should().Be("http");
		uri.Host.Should().Be("localhost");
		uri.Port.Should().BeGreaterThan(0);
		uri.AbsolutePath.Should().Be("/authentication-callback");
	}

	[TestMethod]
	public void GetCurrentApplicationCallbackUri_ReturnsSamePortOnRepeatedCalls()
	{
		var provider = new SkiaWebAuthenticationBrokerProvider();

		var uri1 = provider.GetCurrentApplicationCallbackUri();
		var uri2 = provider.GetCurrentApplicationCallbackUri();

		uri1.Port.Should().Be(uri2.Port);
	}

	[TestMethod]
	public void GetCurrentApplicationCallbackUri_DifferentInstances_MayReturnDifferentPorts()
	{
		var provider1 = new SkiaWebAuthenticationBrokerProvider();
		var provider2 = new SkiaWebAuthenticationBrokerProvider();

		var uri1 = provider1.GetCurrentApplicationCallbackUri();
		var uri2 = provider2.GetCurrentApplicationCallbackUri();

		// Both should have valid ports (may or may not be different)
		uri1.Port.Should().BeGreaterThan(0);
		uri2.Port.Should().BeGreaterThan(0);
	}

	[TestMethod]
	public void OwnerConstructor_Works()
	{
		var provider = new SkiaWebAuthenticationBrokerProvider(owner: new object());

		var uri = provider.GetCurrentApplicationCallbackUri();

		uri.Should().NotBeNull();
		uri.Host.Should().Be("localhost");

	[TestMethod]
	public async Task AuthenticateAsync_ReturnsSuccess_WhenCallbackReceived()
	{
		var provider = new SkiaWebAuthenticationBrokerProvider();
		var callbackUri = provider.GetCurrentApplicationCallbackUri();

		// Build a fake request URI with redirect_uri pointing to our callback
		var requestUri = new Uri($"https://idp.example.com/authorize?client_id=test&redirect_uri={Uri.EscapeDataString(callbackUri.ToString())}");

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

		// Start auth (it will try to open a browser which will fail, but the listener starts)
		var authTask = provider.AuthenticateAsync(
			WebAuthenticationOptions.None,
			requestUri,
			callbackUri,
			cts.Token);

		// Simulate the IdP callback by sending an HTTP request to the listener
		using var client = new HttpClient();
		var callbackUrl = $"{callbackUri}?code=test_auth_code&state=test_state";

		// Small delay to let the listener start
		await Task.Delay(500);

		var response = await client.GetAsync(callbackUrl, cts.Token);
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var result = await authTask;

		result.ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
		result.ResponseData.Should().Contain("code=test_auth_code");
		result.ResponseData.Should().Contain("state=test_state");
	}

	[TestMethod]
	public async Task AuthenticateAsync_FiltersFaviconRequests()
	{
		var provider = new SkiaWebAuthenticationBrokerProvider();
		var callbackUri = provider.GetCurrentApplicationCallbackUri();

		var requestUri = new Uri($"https://idp.example.com/authorize?redirect_uri={Uri.EscapeDataString(callbackUri.ToString())}");

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

		var authTask = provider.AuthenticateAsync(
			WebAuthenticationOptions.None,
			requestUri,
			callbackUri,
			cts.Token);

		await Task.Delay(500);

		using var client = new HttpClient();

		// First send a favicon request - should be ignored (404)
		var port = callbackUri.Port;
		var faviconResponse = await client.GetAsync($"http://localhost:{port}/favicon.ico", cts.Token);
		faviconResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

		// Then send the real callback - should succeed
		var callbackResponse = await client.GetAsync($"{callbackUri}?code=real_code", cts.Token);
		callbackResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var result = await authTask;
		result.ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
		result.ResponseData.Should().Contain("code=real_code");
	}

	[TestMethod]
	public async Task AuthenticateAsync_ReturnsUserCancel_WhenCancelled()
	{
		var provider = new SkiaWebAuthenticationBrokerProvider();
		var callbackUri = provider.GetCurrentApplicationCallbackUri();

		var requestUri = new Uri($"https://idp.example.com/authorize?redirect_uri={Uri.EscapeDataString(callbackUri.ToString())}");

		using var cts = new CancellationTokenSource();

		var authTask = provider.AuthenticateAsync(
			WebAuthenticationOptions.None,
			requestUri,
			callbackUri,
			cts.Token);

		await Task.Delay(500);

		// Cancel the operation
		cts.Cancel();

		var result = await authTask;
		result.ResponseStatus.Should().Be(WebAuthenticationStatus.UserCancel);
	}

	[TestMethod]
	public void ResponseHtmlProvider_CanBeOverridden()
	{
		var original = SkiaWebAuthenticationBrokerProvider.ResponseHtmlProvider;

		try
		{
			SkiaWebAuthenticationBrokerProvider.ResponseHtmlProvider = success =>
				success ? "<html>Custom OK</html>" : "<html>Custom Fail</html>";

			var html = SkiaWebAuthenticationBrokerProvider.ResponseHtmlProvider(true);
			html.Should().Be("<html>Custom OK</html>");

			html = SkiaWebAuthenticationBrokerProvider.ResponseHtmlProvider(false);
			html.Should().Be("<html>Custom Fail</html>");
		}
		finally
		{
			SkiaWebAuthenticationBrokerProvider.ResponseHtmlProvider = original;
		}
	}

	[TestMethod]
	public async Task AuthenticateAsync_FiltersNonGetRequests()
	{
		var provider = new SkiaWebAuthenticationBrokerProvider();
		var callbackUri = provider.GetCurrentApplicationCallbackUri();

		var requestUri = new Uri($"https://idp.example.com/authorize?redirect_uri={Uri.EscapeDataString(callbackUri.ToString())}");

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

		var authTask = provider.AuthenticateAsync(
			WebAuthenticationOptions.None,
			requestUri,
			callbackUri,
			cts.Token);

		await Task.Delay(500);

		using var client = new HttpClient();
		var port = callbackUri.Port;

		// POST to callback path - should be filtered (404)
		var postResponse = await client.PostAsync(
			$"{callbackUri}?code=post_code",
			new StringContent(""),
			cts.Token);
		postResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

		// GET to callback path - should succeed
		var getResponse = await client.GetAsync($"{callbackUri}?code=get_code", cts.Token);
		getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		var result = await authTask;
		result.ResponseStatus.Should().Be(WebAuthenticationStatus.Success);
		result.ResponseData.Should().Contain("code=get_code");
	}
}
