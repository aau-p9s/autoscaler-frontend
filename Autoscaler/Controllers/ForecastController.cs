using Autoscaler.Lib.Autoscaler;
using Autoscaler.Lib.Database;
using Microsoft.AspNetCore.Mvc;

namespace Autoscaler.Controllers;

[ApiController]
[Route("/forecast")]
public class ForecastController : ControllerBase {
    private readonly ILogger<ForecastController> _logger;
    readonly Database Database;

    public ForecastController(Database database) {
        Database = database;
    }

    [HttpGet]
    public async Task<IActionResult> Get() {
        return Ok(new Tuple<Dictionary<DateTime, int>, Dictionary<DateTime, int>> (
            Database.Historic(DateTime.Now.AddDays(-3)),
            Database.Prediction(DateTime.Now.AddDays(3))
        ));
    }
}