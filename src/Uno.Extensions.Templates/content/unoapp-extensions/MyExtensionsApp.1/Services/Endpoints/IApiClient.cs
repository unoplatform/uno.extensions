using System.Collections.Immutable;
using MyExtensionsApp._1.DataContracts;

namespace MyExtensionsApp._1.Services.Endpoints;

[Headers("Content-Type: application/json")]
public interface IApiClient
{
	[Get("/api/weatherforecast")]
	Task<ApiResponse<IImmutableList<WeatherForecast>>> GetWeather(CancellationToken cancellationToken = default);
}
