using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAPI.Models;
using UserAPI.Repositories.Interfaces;

namespace UserAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    IAdminRepository _adminRepository;
    ILogger<AdminController> _logger;
    
    public AdminController(
        ILogger<AdminController> logger,
        IAdminRepository adminRepository)
    {
        _logger = logger;
        _adminRepository = adminRepository;
    }
    
    
    [HttpPost]
    [Route("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TryLoginAdmin(LoginDto credentials)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(TryLoginAdmin));
        if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Password))
        {
            _logger.LogInformation("Login request failed");
            return BadRequest("Login request needs username and password");
        }
        
        var result = await _adminRepository.TryLogin(credentials.Username, credentials.Password);
        if (result == null)
        {
            _logger.LogInformation("Login request for user: {Username} failed with wrong password", credentials.Username);
            return Unauthorized("Invalid credentials");
        }
        _logger.LogInformation("Login request for user: {Username} successful, returning JWT token", credentials.Username);
        return Ok(result);
    }
    
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [Route("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAdmin(AdminDto admin)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(CreateAdmin));
        if (string.IsNullOrWhiteSpace(admin.Username)
            || string.IsNullOrWhiteSpace(admin.Password))
        {
            _logger.LogInformation("Create request failed");
            return BadRequest("Username and Password are required");
        }
        _logger.LogInformation("Creating admin: {Username}", admin.Username);
        var wasAdded = await _adminRepository.CreateNewAdmin(admin);
        if (wasAdded)
            return Created();
        
        _logger.LogInformation("Admin creation failed. Admin: {Username} already exists", admin.Username);
        return BadRequest("Admin already exists");
    }
}