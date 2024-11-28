using Microsoft.AspNetCore.Mvc;

namespace autoscaler_frontend.Controllers;

[ApiController]
[Route("settings")]
public class SettingsController : ControllerBase {
    [HttpPost]
    public async Task<IActionResult> Set([FromBody]Settings settings) {
        Console.WriteLine(settings.ScaleUp);
        Database.Singleton.SetSettings(settings);

        return Ok();
    }
    [HttpGet]
    public async Task<IActionResult> Get() {
        var settings = Database.Singleton.GetSettings();
        return Ok(settings);
    }
}