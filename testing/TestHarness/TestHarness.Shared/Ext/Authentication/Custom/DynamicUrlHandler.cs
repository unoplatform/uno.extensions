using System.Net.Http;

namespace TestHarness.Ext.Authentication.Custom;

internal class DynamicUrlHandler: DelegatingHandler
{
	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var url = request.RequestUri;
		if (url?.OriginalString.Contains("Dummy")??false)
		{
			request.RequestUri = new Uri(url.OriginalString.ToLower());
		}

		return base.SendAsync(request, cancellationToken);
	}

}
