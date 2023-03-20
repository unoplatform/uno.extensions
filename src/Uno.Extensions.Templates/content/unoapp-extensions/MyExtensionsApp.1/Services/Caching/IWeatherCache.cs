using MyExtensionsApp._1.DataContracts;
using System.Collections.Immutable;

namespace MyExtensionsApp._1.Services.Caching;

public interface IWeatherCache
{
    ValueTask<IImmutableList<WeatherForecast>> GetForecast(CancellationToken token);
}
