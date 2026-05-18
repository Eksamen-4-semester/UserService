using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UserAPI.Models;
using UserAPI.Repositories.Interfaces;

namespace UserAPI.Repositories;

public class PersonalTrainerRepositoryMongoDb : IPersonalTrainerRepository
{
    ILogger<PersonalTrainerRepositoryMongoDb> _logger;
    IMongoCollection<PersonalTrainer> _personalTrainerCollection;
    IHttpClientFactory _httpClientFactory;
    public PersonalTrainerRepositoryMongoDb(
        IHttpClientFactory httpClientFactory,
        ILogger<PersonalTrainerRepositoryMongoDb> logger,
        IMongoDatabase database)
    {
        _logger = logger;
        _personalTrainerCollection = database.GetCollection<PersonalTrainer>("PersonalTrainer");
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<string?> TryLogin(string username, string password)
    {
        _logger.LogDebug($"TryLogin called from {nameof(PersonalTrainerRepositoryMongoDb)}");

        try
        {
            var trainer = await GetPersonalTrainerByUsername(username);
            if (trainer == null)
            {
                _logger.LogInformation("TryLogin failed, trainer not found");
                return null;
            }
            PasswordHasher<PersonalTrainer> passwordHasher = new PasswordHasher<PersonalTrainer>();
        
            var hashedPassword = passwordHasher.VerifyHashedPassword(trainer, trainer.Password, password);
            if (hashedPassword == PasswordVerificationResult.Failed)
            {
                _logger.LogInformation(
                    "TryLogin failed, invalid password for trainer {trainerId}",
                    trainer.Username);
                return null;
            }

            _logger.LogInformation(
                "TryLogin succeeded for trainer {trainerId}",
                trainer.Username);
            
            _logger.LogInformation("Getting JWT for trainer {Username}", trainer.Username);
            var authClient = _httpClientFactory.CreateClient("authService");
            var auths = new AuthAdminDto(trainer.PersonalTrainerId, trainer.Username);
            
            var jwtToken = await authClient.PostAsJsonAsync("api/auth/trainer", auths);
            if (jwtToken.IsSuccessStatusCode)
                return await jwtToken.Content.ReadAsStringAsync();
            
            _logger.LogError("Authservice failed to provide JWT token");
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "TryLogin failed due to an exception");
            return null;
        }
    }

    public async Task<bool> CreateNewPersonalTrainer(PersonalTrainerDto trainer)
    {
        _logger.LogDebug($"CreateNewPersonalTrainer called from {nameof(PersonalTrainerRepositoryMongoDb)}");

        try
        {
            var trainerAlreadyExists = await GetPersonalTrainerByUsername(trainer.Username);
            if (trainerAlreadyExists != null)
                return false;
            int newMax = await GetMaxId() + 1;
            
            var dbTrainer = new PersonalTrainer()
            {
                PersonalTrainerId = newMax,
                Username = trainer.Username,
                Password = trainer.Password,
                FullName = trainer.FullName
            };
            
            PasswordHasher<PersonalTrainer> passwordHasher = new PasswordHasher<PersonalTrainer>();
            var hashedPassword = passwordHasher.HashPassword(dbTrainer, dbTrainer.Password);
            dbTrainer.Password = hashedPassword;
            
            await _personalTrainerCollection.InsertOneAsync(dbTrainer);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreateNewAdmin failed");
            return false;
        }
    }

    public async Task<PersonalTrainer?> GetPersonalTrainerById(int id)
    {
        var filter = Builders<PersonalTrainer>.Filter.Eq("_id", id);
        var projection = Builders<PersonalTrainer>.Projection.Exclude("Password");
        return await _personalTrainerCollection.Find(filter).Project<PersonalTrainer>(projection).FirstOrDefaultAsync();
    }
    
    private async Task<PersonalTrainer?> GetPersonalTrainerByUsername(string username)
    {
        var filter = Builders<PersonalTrainer>.Filter.Eq(x => x.Username, username);
        var result = await _personalTrainerCollection.Find(filter).FirstOrDefaultAsync();
        return result;
    }
    
    private async Task<int> GetMaxId()
    {
        _logger.LogDebug("GetMaxId called from MemberRepositoryMongoDb");
        
        try
        {
            var filter = Builders<PersonalTrainer>.Filter.Empty;
            var sort = Builders<PersonalTrainer>.Sort.Descending("_id");
        
            var result = await _personalTrainerCollection.Find(filter).Sort(sort).Limit(1).FirstOrDefaultAsync();
            var maxId = result?.PersonalTrainerId ?? 0;

            _logger.LogDebug(
                "GetMaxId returned {MaxId} from PersonalTrainer collection",
                maxId);

            return maxId;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "GetMaxId failed from PersonalTrainer collection");

            return 0;
        }
    }
}