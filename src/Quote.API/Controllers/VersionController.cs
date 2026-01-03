using Microsoft.AspNetCore.Mvc;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public VersionController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Get the current application version
    /// </summary>
    [HttpGet]
    public ActionResult<VersionInfo> GetVersion()
    {
        return Ok(new VersionInfo(
            Name: _configuration["App:Name"] ?? "Quote",
            Version: _configuration["App:Version"] ?? "0.0.0",
            Description: _configuration["App:Description"] ?? ""
        ));
    }
}

public record VersionInfo(string Name, string Version, string Description);
