using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserAPI.Models;
using UserAPI.Models.ResultObjects;
using UserAPI.Repositories.Interfaces;

namespace UserAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MemberController : ControllerBase
{
    IMemberRepository _memberRepository;
    ILogger<MemberController> _logger;
    
    public MemberController(
        IMemberRepository memberRepository,
        ILogger<MemberController> logger)
    {
        _memberRepository = memberRepository;
        _logger = logger;
    }

    [HttpPost]
    [Route("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TryLoginMember(LoginDto credentials)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(TryLoginMember));
        if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Password))
        {
            _logger.LogInformation("Login request failed");
            return BadRequest("Login request needs username and password");
        }
        
        var result = await _memberRepository.TryLogin(credentials.Username, credentials.Password);
        if (result == null)
        {
            _logger.LogInformation("Login request for user: {Username} failed with wrong password", credentials.Username);
            return Unauthorized("Invalid credentials");
        }
        _logger.LogInformation("Login request for user: {Username} successful, returning JWT token", credentials.Username);
        return Ok(result);
    }
    
    [HttpPost]
    [Route("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateMember(MemberDto member)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(CreateMember));
        if (string.IsNullOrWhiteSpace(member.FullName)
            || string.IsNullOrWhiteSpace(member.Username)
            || string.IsNullOrWhiteSpace(member.Password))
        {
            _logger.LogInformation("Create request failed");
            return BadRequest("Full name, Username and Password are required");
        }

        if (member.DOB > DateTime.UtcNow)
        {
            _logger.LogInformation("Create request failed, DOB cannot be greater than current time");
            return BadRequest("DOB cannot be greater than current time");
        }
        if (member.DOB == null)
        {
            _logger.LogInformation("Create request failed, DOB cannot be null");
            return BadRequest("DOB must exist");
        }
        _logger.LogInformation("Creating member: {Username}", member.Username);
        var wasAdded = await _memberRepository.CreateNewMember(member);
        if (wasAdded)
            return Created();
        
        _logger.LogInformation("Member creation failed. Member: {Username} already exists", member.Username);
        return BadRequest("Member already exists");
    }

    [Authorize]
    [HttpGet]
    [Route("{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMemberById(int memberId)
    {
        _logger.LogInformation("Called {function} endpoint", nameof(GetMemberById));
        if (memberId <= 0)
        {
            _logger.LogInformation("{function} called with invalid member id", nameof(GetMemberById));
            return BadRequest("Invalid member id");
        }
        var member = await _memberRepository.GetMemberById(memberId);
        if (member == null)
        {
            _logger.LogInformation("Member with member id: {id} does not exist", memberId);
            return NotFound();
        }
        return Ok(member);
    }
    
    [Authorize]
    [HttpGet]
    [Route("own")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOwnMemberByJwt()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            _logger.LogError("Users id claim not found in token");
            return Unauthorized();
        }
        
        var memberId = int.Parse(userIdClaim.Value);
        
        _logger.LogInformation("Called {function} endpoint", nameof(GetOwnMemberByJwt));
        if (memberId <= 0)
        {
            _logger.LogInformation("{function} called with invalid user id", nameof(GetOwnMemberByJwt));
            return BadRequest("Invalid user id");
        }
        var member = await _memberRepository.GetMemberById(memberId);
        if (member == null)
        {
            _logger.LogInformation("Member with user id: {id} does not exist", memberId);
            return NotFound();
        }
        return Ok(member);
    }
    
    [Authorize(Roles = "Admin,Trainer")]
    [HttpPut]
    [Route("deactivate/{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateMember(int memberId)
    {
        if (memberId <= 0)
        {
            _logger.LogInformation("{function} called with invalid member id", nameof(DeactivateMember));
            return BadRequest("Invalid member id");
        }
        
        var result = await _memberRepository.DeactivateMember(memberId);
        if (result.ResultType == ActivationResultType.InternalError)
        {
            _logger.LogError("Deactivation request failed");
            return Conflict(result.ErrorMessage);
        }

        if (result.ResultType == ActivationResultType.NotFound)
        {
            _logger.LogError("Member with  member id: {id} does not exist", memberId);
            return NotFound(result.ErrorMessage);
        }

        if (result.ResultType == ActivationResultType.NoChange)
        {
            _logger.LogError("Member with member id: {id} is already deactivated", memberId);
            return BadRequest(result.ErrorMessage);
        }

        if (result.ResultType == ActivationResultType.Success)
        {
            _logger.LogInformation("Deactivated member: {id}", memberId);
            return Accepted();
        }

        throw new ApplicationException("Unknown activation result type");
    }
    
    [Authorize(Roles = "Admin,Trainer")]
    [HttpPut]
    [Route("activate/{memberId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActivateMember(int memberId)
    {
        if (memberId <= 0)
        {
            _logger.LogInformation("{function} called with invalid member id", nameof(ActivateMember));
            return BadRequest("Invalid member id");
        }
        
        var result = await _memberRepository.ActivateMember(memberId);
        if (result.ResultType == ActivationResultType.InternalError)
        {
            _logger.LogError("Activated request failed");
            return Conflict(result.ErrorMessage);
        }

        if (result.ResultType == ActivationResultType.NotFound)
        {
            _logger.LogError("Member with  member id: {id} does not exist", memberId);
            return NotFound(result.ErrorMessage);
        }

        if (result.ResultType == ActivationResultType.NoChange)
        {
            _logger.LogError("Member with member id: {id} is already activated", memberId);
            return BadRequest(result.ErrorMessage);
        }

        if (result.ResultType == ActivationResultType.Success)
        {
            _logger.LogInformation("Activated member: {id}", memberId);
            return Accepted();
        }

        throw new ApplicationException("Unknown activation result type");
        
    }
    
}