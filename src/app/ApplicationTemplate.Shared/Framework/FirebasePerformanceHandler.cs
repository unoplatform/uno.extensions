//-:cnd:noEmit
#if __ANDROID__
//+:cnd:noEmit
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationTemplate.Framework
{
	public class FirebasePerformanceHandler : DelegatingHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
		{
			if (request is null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			var metric = Firebase.Perf.FirebasePerformance.Instance.NewHttpMetric(request.RequestUri.AbsoluteUri, request.Method.Method.ToUpperInvariant());

			try
			{
				metric.Start();

				// Make sure to store request information before the API call has been made. Otherwise, the request may have been disposed.
				metric.SetRequestPayloadSize(request.Content?.Headers?.ContentLength ?? 0);

				var response = await base.SendAsync(request, ct);

				metric.SetHttpResponseCode((int)response.StatusCode);
				metric.SetResponseContentType(response.Content?.Headers?.ContentType?.MediaType);
				metric.SetResponsePayloadSize(response.Content?.Headers?.ContentLength ?? 0);

				return response;
			}
			finally
			{
				metric.Stop();
			}
		}
	}
}
//-:cnd:noEmit
#endif
//+:cnd:noEmit
