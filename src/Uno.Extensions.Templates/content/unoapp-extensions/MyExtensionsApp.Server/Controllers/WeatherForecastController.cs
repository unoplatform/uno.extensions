//-:cnd:noEmit
namespace MyExtensionsApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherForecastController : ControllerBase
{
	private static readonly string[] Summaries = new[]
	{
		"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
	};

	private readonly ILogger<WeatherForecastController> _logger;

	public WeatherForecastController(ILogger<WeatherForecastController> logger)
	{
		_logger = logger;
	}

	/// <summary>
	/// Creates a make believe weather forecast for the next 5 days.
	/// </summary>
	/// <remarks>A 5 Day Forecast</remarks>
	/// <response code="200">Weather Forecast returned</response>
	[HttpGet(Name = "GetWeatherForecast")]
	[ProducesResponseType(typeof(IEnumerable<WeatherForecast>), 200)]
	public IEnumerable<WeatherForecast> Get() =>
		Enumerable.Range(1, 5).Select(index =>
			new WeatherForecast(
				DateTime.Now.AddDays(index),
				Random.Shared.Next(-20, 55),
				Summaries[Random.Shared.Next(Summaries.Length)]
			)
		)
		.Select(x => {
			_logger.LogInformation("Weather forecast for {Date} is a {Summary} {TemperatureC}°C", x.Date, x.Summary, x.TemperatureC);
			return x;
		})
		.ToArray();
}
