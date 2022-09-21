using Microsoft.AspNetCore.Mvc;

using SurrealDB.Abstractions;
using SurrealDB.Driver.Rpc;

namespace MinimalApi.Controllers;

[ApiController, Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly IDatabase _db;
    private readonly ILogger<WeatherForecastController> _logger;

    public WeatherForecastController(IDatabase db, ILogger<WeatherForecastController> logger) {
        _db = db;
        _logger = logger;
    }
    
    [HttpPost(Name = "WeatherForecast")]
    public async Task Create(CancellationToken ct = default) {
        await _db.Open(ct);
        
        WeatherForecast[] create = Enumerable.Range(1, 5).Select(
            static index => new WeatherForecast {
                Date = DateTime.Now.AddDays(index), TemperatureC = Random.Shared.Next(-20, 55), Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            }
        ).ToArray();

        await _db.Create("weather", create, ct);
    }

    [HttpGet(Name = "WeatherForecast")]
    public async Task<ActionResult<WeatherForecast?>> Get(CancellationToken ct = default) {
        await _db.Open(ct);

        var query = await _db.Select("weather", ct);
        if (query.TryGetResult(out var res)) {
            return res.GetObject<WeatherForecast>();
        }

        return new NoContentResult();
    }
}
