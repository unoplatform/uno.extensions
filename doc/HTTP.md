# HTTP
_[TBD - Review and update this guidance]_

We use [Microsoft.Extensions.Http](https://www.nuget.org/packages/Microsoft.Extensions.Http) for any HTTP related work.

For more documentation on HTTP requests, read the references listed at the bottom.

## HTTP endpoints

- You can register a service with a dependency to `HttpClient` using `services.AddHttpClient<IEndpoint, EndpointImplementation>()` in the [ApiConfiguration.cs](../src/app/ApplicationTemplate.Shared/Configuration/ApiConfiguration.cs) file.

- We use `DelegatingHandler` to create HTTP request / response pipelines. There are lot of delegating handlers implementation in the community, we provide some in [MallardMessageHandlers](https://github.com/nventive/MallardMessageHandlers).

- We use [Refit](https://www.nuget.org/packages/Refit/) to generate the implementation of the client layer.

## Caching and policies

- We may use [Microsoft.Extensions.Http.Polly](https://www.nuget.org/packages/Microsoft.Extensions.Http.Polly/) to leverage request policies (e.g. retry, timeout, etc.).

- We may use [MonkeyCache](https://github.com/jamesmontemagno/monkey-cache) to leverage API response caching.

## Mocking

- We use a simple `BaseMock` class to support mocking scenarios. You simply add embbedded resources (.json files) that contain the mocked responses into your project.

## References
- [Making HTTP requests using IHttpClientFactory](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.0)
- [Delegating handlers](https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/http-message-handlers)
- [Polly and HttpClientFactory](https://github.com/App-vNext/Polly/wiki/Polly-and-HttpClientFactory)
- [What is Refit](https://github.com/reactiveui/refit)
