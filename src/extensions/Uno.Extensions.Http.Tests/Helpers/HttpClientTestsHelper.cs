using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Http.Handlers.Tests
{
	public static class HttpClientTestsHelper
	{
		private const string TestHttpClientName = nameof(TestHttpClientName);
		private const string TestHttpClientName2 = nameof(TestHttpClientName2);

		public static IHttpClientFactory GetHttpClientFactory(Action<ServiceCollection> serviceCollectionBuilder)
		{
			var serviceProvider = GetServiceProvider(serviceCollectionBuilder);

			return serviceProvider.GetService<IHttpClientFactory>();
		}

		public static IServiceProvider GetServiceProvider(Action<ServiceCollection> serviceCollectionBuilder)
		{
			var serviceCollection = new ServiceCollection();

			serviceCollectionBuilder(serviceCollection);

			return serviceCollection.BuildServiceProvider();
		}

		public static HttpClient GetTestHttpClient(
			Action<ServiceCollection> serviceCollectionBuilder,
			Action<IHttpClientBuilder> httpClientBuilder
		)
		{
			var httpClientFactory = GetHttpClientFactory(s =>
			{
				serviceCollectionBuilder(s);

				httpClientBuilder(s.AddHttpClient(TestHttpClientName));
			});

			return httpClientFactory.CreateClient(TestHttpClientName);
		}

		public static (HttpClient client1, HttpClient client2) GetTestHttpClients(
			Action<ServiceCollection> serviceCollectionBuilder,
			Action<IHttpClientBuilder> httpClientBuilder
		)
		{
			var httpClientFactory = GetHttpClientFactory(s =>
			{
				serviceCollectionBuilder(s);

				httpClientBuilder(s.AddHttpClient(TestHttpClientName));
				httpClientBuilder(s.AddHttpClient(TestHttpClientName2));
			});

			return (httpClientFactory.CreateClient(TestHttpClientName), httpClientFactory.CreateClient(TestHttpClientName2));
		}

		public static HttpResponseMessage CreateHttpResponseMessage(TestResponse response, HttpStatusCode statusCode)
		{
			var serializedResult = JsonSerializer.Serialize(response);

			return new HttpResponseMessage()
			{
				Content = new StringContent(serializedResult),
				StatusCode = statusCode
			};
		}
	}
}
