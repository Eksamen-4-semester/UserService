using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UserAPI.Models;
using UserAPI.Repositories.Interfaces;

namespace UserAPI.Repositories;

public class AdminRepositoryMongoDb : IAdminRepository
{
    
    private readonly ILogger<AdminRepositoryMongoDb> _logger;
    private readonly IMongoCollection<Admin> _adminCollection;
    private readonly IHttpClientFactory _httpClientFactory;
    
    public AdminRepositoryMongoDb(
        ILogger<AdminRepositoryMongoDb> logger,
        IMongoDatabase mongoDatabase,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _adminCollection = mongoDatabase.GetCollection<Admin>("Admins");
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<string?> TryLogin(string username, string password)
    {
        _logger.LogDebug($"TryLogin called from {nameof(AdminRepositoryMongoDb)}");

        try
        {
            var admin = await GetAdminByUsername(username);
            if (admin == null)
            {
                _logger.LogInformation("TryLogin failed, admin not found");
                return null;
            }
            PasswordHasher<Admin> passwordHasher = new PasswordHasher<Admin>();
        
            var hashedPassword = passwordHasher.VerifyHashedPassword(admin, admin.Password, password);
            if (hashedPassword == PasswordVerificationResult.Failed)
            {
                _logger.LogInformation(
                    "TryLogin failed, invalid password for admin {AdminId}",
                    admin.Username);
                return null;
            }

            _logger.LogInformation(
                "TryLogin succeeded for admin {MemberId}",
                admin.Username);
            
            _logger.LogInformation("Getting JWT for admin {Username}", admin.Username);
            var authClient = _httpClientFactory.CreateClient("authService");
            var auths = new AuthAdminDto(admin.AdminId, admin.Username);
            
            var jwtToken = await authClient.PostAsJsonAsync("api/auth/admin", auths);
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

    public async Task<bool> CreateNewAdmin(AdminDto admin)
    {
        _logger.LogDebug($"CreateNewAdmin called from {nameof(AdminRepositoryMongoDb)}");

        try
        {
            var adminAlreadyExists = await GetAdminByUsername(admin.Username);
            if (adminAlreadyExists != null)
                return false;
            int newMax = await GetMaxId() + 1;
            
            var dbAdmin = new Admin()
            {
                AdminId = newMax,
                Username = admin.Username,
                Password = admin.Password,
            };
            
            PasswordHasher<Admin> passwordHasher = new PasswordHasher<Admin>();
            var hashedPassword = passwordHasher.HashPassword(dbAdmin, dbAdmin.Password);
            dbAdmin.Password = hashedPassword;
            
            await _adminCollection.InsertOneAsync(dbAdmin);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreateNewAdmin failed");
            return false;
        }
    }

    public Task<Admin?> GetAdminById(int id)
    {
        throw new NotImplementedException();
    }
    
    private async Task<Admin?> GetAdminByUsername(string username)
    {
        var filter = Builders<Admin>.Filter.Eq(x => x.Username, username);
        var result = await _adminCollection.Find(filter).FirstOrDefaultAsync();
        return result;
    }
    
    private async Task<int> GetMaxId()
    {
        _logger.LogDebug("GetMaxId called from AdminRepositoryMongoDb");
        
        try
        {
            var filter = Builders<Admin>.Filter.Empty;
            var sort = Builders<Admin>.Sort.Descending("_id");
        
            var result = await _adminCollection.Find(filter).Sort(sort).Limit(1).FirstOrDefaultAsync();
            var maxId = result?.AdminId ?? 0;

            _logger.LogDebug(
                "GetMaxId returned {MaxId} from Admins collection",
                maxId);

            return maxId;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "GetMaxId failed from Admins collection");

            return 0;
        }
    }
}