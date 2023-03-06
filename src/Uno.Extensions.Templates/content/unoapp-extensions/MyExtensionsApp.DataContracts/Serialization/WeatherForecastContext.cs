using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace MyExtensionsApp.DataContracts.Serialization;

/*
 * When using the JsonSerializerContext you must add the JsonSerializableAttribute
 * for each type that you may need to serialize / deserialize including both the
 * concrete type and any interface that the concrete type implements.
 */
[JsonSerializable(typeof(WeatherForecast[]))]
[JsonSerializable(typeof(IEnumerable<WeatherForecast>))]
[JsonSerializable(typeof(IImmutableList<WeatherForecast>))]
[JsonSerializable(typeof(ImmutableList<WeatherForecast>))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
public partial class WeatherForecastContext : JsonSerializerContext
{
}
