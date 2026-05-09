using Microsoft.AspNetCore.Mvc;
using UserAPI.Models;

namespace UserAPI.Repositories.Interfaces;

public interface IMemberRepository
{
    Task<string?> TryLogin(string username, string password);
    Task<bool> CreateNewMember(MemberDto member);
    Task<Member?> GetMemberById(int id);
}