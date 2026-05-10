using Microsoft.AspNetCore.Mvc;
using UserAPI.Models;
using UserAPI.Models.ResultObjects;

namespace UserAPI.Repositories.Interfaces;

public interface IMemberRepository
{
    Task<string?> TryLogin(string username, string password);
    Task<bool> CreateNewMember(MemberDto member);
    Task<Member?> GetMemberById(int id);
    Task<ActivationResult> DeactivateMember(int id);
    Task<ActivationResult> ActivateMember(int id);
}