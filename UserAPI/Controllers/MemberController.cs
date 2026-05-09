using Microsoft.AspNetCore.Mvc;
using UserAPI.Models;
using UserAPI.Repositories.Interfaces;

namespace UserAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MemberController : ControllerBase
{
    IMemberRepository _memberRepository;
    
    public MemberController(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    [HttpPost]
    [Route("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TryLogin(LoginDto credentials)
    {
        if (string.IsNullOrWhiteSpace(credentials.Username) || string.IsNullOrWhiteSpace(credentials.Password))
         return BadRequest("Login request needs username and password");
        
        var result = await _memberRepository.TryLogin(credentials.Username, credentials.Password);
        if (result == null)
            return Unauthorized("Invalid credentials");
        
        return Ok(result);
    }
    
    [HttpPost]
    [Route("create")]
    public async Task<IActionResult> CreateMember(MemberDto member)
    {
        if (string.IsNullOrWhiteSpace(member.FullName)
            || string.IsNullOrWhiteSpace(member.Username)
            || string.IsNullOrWhiteSpace(member.Password))
        {
            return BadRequest("Full name, Username and Password are required");
        }
        
        var wasAdded = await _memberRepository.CreateNewMember(member);
        if (wasAdded)
            return Created();
        
        return BadRequest("Failed to create new member");
    }
    
}