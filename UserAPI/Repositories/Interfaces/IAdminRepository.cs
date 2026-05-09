using Microsoft.AspNetCore.Mvc;
using UserAPI.Models;

namespace UserAPI.Repositories.Interfaces;

public interface IAdminRepository
{
    Task<string?> TryLogin(string username, string password);
    Task<bool> CreateNewAdmin(AdminDto member);
    Task<Admin?> GetAdminById(int id);
}