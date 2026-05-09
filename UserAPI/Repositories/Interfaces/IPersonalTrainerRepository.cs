using Microsoft.AspNetCore.Mvc;
using UserAPI.Models;

namespace UserAPI.Repositories.Interfaces;

public interface IPersonalTrainerRepository
{
    Task<string?> TryLogin(string username, string password);
    Task<bool> CreateNewPersonalTrainer(PersonalTrainerDto member);
    Task<PersonalTrainer?> GetPersonalTrainerById(int id);
}