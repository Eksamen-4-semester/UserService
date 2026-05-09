using Microsoft.AspNetCore.Mvc;
using UserAPI.Models;
using UserAPI.Repositories.Interfaces;

namespace UserAPI.Repositories;

public class AdminRepositoryMongoDb : IAdminRepository
{
    public Task<string?> TryLogin(string username, string password)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateNewAdmin(AdminDto member)
    {
        throw new NotImplementedException();
    }

    public Task<Admin?> GetAdminById(int id)
    {
        throw new NotImplementedException();
    }
}