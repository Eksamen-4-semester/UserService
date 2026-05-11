using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace UserAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    private readonly ILogger<VersionController> _logger;

    public VersionController(
        ILogger<VersionController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public async Task<Dictionary<string,string>> GetVersion()
    {
        _logger.LogInformation("Version controll endpoint called for AuthService");
        var properties = new Dictionary<string, string>();
        var assembly = typeof(Program).Assembly;
        properties.Add("service", "FitLife Auth service");
        var ver = FileVersionInfo.GetVersionInfo(typeof(Program)
            .Assembly.Location).ProductVersion;
        properties.Add("version", ver!);
        try {
            var hostName = System.Net.Dns.GetHostName();
            var ips = await System.Net.Dns.GetHostAddressesAsync(hostName);
            var ipa = ips.First().MapToIPv4().ToString();
            properties.Add("hosted-at-address", ipa);
        } catch (Exception ex) {
            _logger.LogError(ex.Message);
            properties.Add("hosted-at-address", "Could not resolve IP-address");
        }
        return properties;
    }
}