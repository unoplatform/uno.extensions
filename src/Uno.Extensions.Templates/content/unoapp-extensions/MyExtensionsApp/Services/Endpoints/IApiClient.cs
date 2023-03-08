using System.Collections.Immutable;
using MyExtensionsApp.DataContracts;

namespace MyExtensionsApp.Services.Endpoints;

[Headers("Content-Type: application/json")]
public interface IApiClient
{
	[Get("/api/weatherforecast")]
	Task<ApiResponse<IImmutableList<WeatherForecast>>> GetWeather(CancellationToken cancellationToken = default);
}
