using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Uno.Extensions.Http.Handlers;
using Xunit;

namespace Uno.Extensions.Http.Handlers.Tests
{
	public class ErrorResponseInterpreterHandlerTests
	{
		private const string DefaultRequestUri = "http://wwww.test.com";

		[Theory]
		[InlineData(HttpStatusCode.BadRequest, true, true, true)] // Fail-Data-Error-Exception
		[InlineData(HttpStatusCode.BadRequest, true, false, false)] // Fail-Data-NoError-NoException
		[InlineData(HttpStatusCode.BadRequest, false, true, true)] // Fail-NoData-Error-Exception
		[InlineData(HttpStatusCode.BadRequest, false, false, false)] // Fail-NoData-NoError-NoException
		[InlineData(HttpStatusCode.OK, true, true, false)] // Success-Data-Error-NoException
		[InlineData(HttpStatusCode.OK, true, false, false)] // Success-Data-NoError-NoException
		[InlineData(HttpStatusCode.OK, false, true, false)] // Success-NoData-Error-NoException
		[InlineData(HttpStatusCode.OK, false, false, false)] // Success-NoData-NoError-NoException
		public async Task It_Interprets_Response(HttpStatusCode statusCode, bool hasData, bool hasError, bool shouldThrow)
		{
			var data = hasData ? new TestData("Test content") : null;
			var error = hasError ? new TestError(1, "Test error") : null;
			var result = new TestResponse(data, error);

			var httpResponse = HttpClientTestsHelper.CreateHttpResponseMessage(result, statusCode);

			var errorResponseInterpreter = new ErrorResponseInterpreter<TestResponse>(
				(request, response, deserializedResponse) => deserializedResponse.Error != null,
				(request, response, deserializedResponse) => new TestException(deserializedResponse.Error)
			);

			void BuildServices(IServiceCollection s) => s
				.AddSingleton<IResponseContentDeserializer>(new ResponseContentDeserializer())
				.AddSingleton<IErrorResponseInterpreter<TestResponse>>(errorResponseInterpreter)
				.AddTransient<ExceptionInterpreterHandler<TestResponse>>()
				.AddTransient(_ => new TestHandler((r, ct) => Task.FromResult(httpResponse)));

			void BuildHttpClient(IHttpClientBuilder h) => h
				.AddHttpMessageHandler<ExceptionInterpreterHandler<TestResponse>>()
				.AddHttpMessageHandler<TestHandler>();

			var httpClient = HttpClientTestsHelper.GetTestHttpClient(BuildServices, BuildHttpClient);

			if (shouldThrow)
			{
				await Assert.ThrowsAsync<TestException>(() => httpClient.GetAsync(DefaultRequestUri));
			}
			else
			{
				var response = await httpClient.GetAsync(DefaultRequestUri);

				response.Should().Be(httpResponse);
			}
		}

		private class ResponseContentDeserializer : IResponseContentDeserializer
		{
			public async Task<TResponse> Deserialize<TResponse>(CancellationToken ct, HttpContent content)
			{
				using (var stream = await content.ReadAsStreamAsync())
				{
					return await JsonSerializer.DeserializeAsync<TResponse>(stream);
				}
			}
		}
	}
}
