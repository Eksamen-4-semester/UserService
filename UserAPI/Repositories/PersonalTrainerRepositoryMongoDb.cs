using Microsoft.AspNetCore.Mvc;
using UserAPI.Models;
using UserAPI.Repositories.Interfaces;

namespace UserAPI.Repositories;

public class PersonalTrainerRepositoryMongoDb : IPersonalTrainerRepository
{
    public Task<string?> TryLogin(string username, string password)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateNewPersonalTrainer(PersonalTrainerDto member)
    {
        throw new NotImplementedException();
    }

    public Task<PersonalTrainer?> GetPersonalTrainerById(int id)
    {
        throw new NotImplementedException();
    }
}