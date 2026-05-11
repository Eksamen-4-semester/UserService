using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAPI.Models;
using UserAPI.Repositories.Interfaces;

namespace UserAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonalTrainerController : ControllerBase
{
    ILogger<PersonalTrainerController> _logger;
    IPersonalTrainerRepository _trainerRepository;

    public PersonalTrainerController(
        ILogger<PersonalTrainerController> logger,
        IPersonalTrainerRepository adminRepository)
    {
        _logger = logger;
        _trainerRepository = adminRepository;
    }
    
    [HttpPost]
    [Route("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TryLoginPersonalTrainer(LoginDto credentials)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(TryLoginPersonalTrainer));
        if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Password))
        {
            _logger.LogInformation("Login request failed");
            return BadRequest("Login request needs username and password");
        }
        
        var result = await _trainerRepository.TryLogin(credentials.Username, credentials.Password);
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
    public async Task<IActionResult> CreatePersonalTrainer([FromBody] PersonalTrainerDto trainer)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(CreatePersonalTrainer));
        if (string.IsNullOrWhiteSpace(trainer.FullName) ||
            string.IsNullOrWhiteSpace(trainer.Username) ||
            string.IsNullOrWhiteSpace(trainer.Password))
        {
            _logger.LogInformation("Login request failed");
            return BadRequest("Registering a personal trainer requires username, password and name");
        }
        _logger.LogInformation("Creating member: {Trainer}", trainer.Username);
        var wasAdded = await _trainerRepository.CreateNewPersonalTrainer(trainer);
        if (wasAdded)
            return Created();
        
        _logger.LogInformation("Trainer creation failed. Trainer: {Username} already exists", trainer.Username);
        return BadRequest("Trainer already exists");
    }
    
}