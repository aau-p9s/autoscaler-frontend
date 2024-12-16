using Microsoft.AspNetCore.Mvc;

namespace Autoscaler.Controllers;

[ApiController]
[Route("settings")]
public class SettingsController : ControllerBase {
    [HttpPost]
    public async Task<IActionResult> Set([FromBody]Settings settings) {
        Console.WriteLine(settings.ScaleUp);
        Autoscaler.Database().SetSettings(settings);

        return Ok();
    }
    [HttpGet]
    public async Task<IActionResult> Get() {
        var settings = Autoscaler.Database().GetSettings();
        return Ok(settings);
    }
}