using Microsoft.AspNetCore.Mvc;

namespace autoscaler_frontend.Controllers;

[ApiController]
[Route("/forecast")]
public class ForecastController : ControllerBase
{

    private readonly ILogger<ForecastController> _logger;

    public ForecastController(ILogger<ForecastController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return Ok(new Tuple<Dictionary<DateTime, int>, Dictionary<DateTime, int>> (Forecaster.Singleton.Historic(), Forecaster.Singleton.Prediction()));
    }
}