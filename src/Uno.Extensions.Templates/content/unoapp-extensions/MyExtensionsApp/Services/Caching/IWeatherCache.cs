using MyExtensionsApp.DataContracts;
using System.Collections.Immutable;

namespace MyExtensionsApp.Services.Caching;

public interface IWeatherCache
{
    ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token);
}
