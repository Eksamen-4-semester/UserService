using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UserAPI.Models;
using UserAPI.Models.ResultObjects;
using UserAPI.Repositories.Interfaces;

namespace UserAPI.Repositories;

public class MemberRepositoryMongoDb : IMemberRepository
{
    IMongoCollection<Member> _memberCollection;
    ILogger<MemberRepositoryMongoDb> _logger;
    IHttpClientFactory _httpClientFactory;
    
    public MemberRepositoryMongoDb(
        IMongoDatabase database,
        ILogger<MemberRepositoryMongoDb> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _memberCollection = database.GetCollection<Member>("Members");
    }
    
    public async Task<string?> TryLogin(string username, string password)
    {
        _logger.LogDebug($"TryLogin called from {nameof(MemberRepositoryMongoDb)}");

        try
        {
            var member = await GetMemberByUsername(username);
            if (member == null)
            {
                _logger.LogInformation("TryLogin failed, member not found");
                return null;
            }
            PasswordHasher<Member> passwordHasher = new PasswordHasher<Member>();
        
            var hashedPassword = passwordHasher.VerifyHashedPassword(member, member.Password, password);
            if (hashedPassword == PasswordVerificationResult.Failed)
            {
                _logger.LogInformation(
                    "TryLogin failed, invalid password for member {MemberId}",
                    member.Username);
                return null;
            }

            _logger.LogInformation(
                "TryLogin succeeded for member {MemberId}",
                member.Username);
            
            _logger.LogInformation("Getting JWT for user {Username}", member.Username);
            var authClient = _httpClientFactory.CreateClient("authService");
            var auths = new AuthMemberDto(member.MemberId, member.Username);
            
            var jwtToken = await authClient.PostAsJsonAsync("api/auth/member", auths);
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

    public async Task<bool> CreateNewMember(MemberDto member)
    {
        _logger.LogDebug($"CreateNewMember called from {nameof(MemberRepositoryMongoDb)}");

        try
        {
            var userAlreadyExists = await GetMemberByUsername(member.Username);
            if (userAlreadyExists != null)
                return false;
            int newMax = await GetMaxId() + 1;
            
            var dbMember = new Member()
            {
                MemberId = newMax,
                Username = member.Username,
                Password = member.Password,
                Active = true,
                DOB = member.DOB,
                FullName = member.FullName,
                SignUpDate = DateTime.UtcNow
            };
            
            PasswordHasher<Member> passwordHasher = new PasswordHasher<Member>();
            var hashedPassword = passwordHasher.HashPassword(dbMember, dbMember.Password);
            dbMember.Password = hashedPassword;
            
            await _memberCollection.InsertOneAsync(dbMember);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreateNewMember failed");
            Console.WriteLine(e);
            return false;
        }
    }

    public async Task<Member?> GetMemberById(int id)
    {
        var filter = Builders<Member>.Filter.Eq("_id", id);
        var projection = Builders<Member>.Projection.Exclude("Password");
        return await _memberCollection.Find(filter).Project<Member>(projection).FirstOrDefaultAsync();
    }

    public async Task<ActivationResult> DeactivateMember(int id)
    {
        var actResult = new ActivationResult();
        var filter = Builders<Member>.Filter.Eq("_id", id);
        var member = await _memberCollection.Find(filter).FirstOrDefaultAsync();
        if (member == null)
        {
            _logger.LogError("DeactivateMember failed, member not found");
            actResult.ResultType = ActivationResultType.NotFound;
            actResult.ErrorMessage = $"Member {id} not found";
            return actResult;
        }

        if (!member.Active)
        {
            _logger.LogError("DeactivateMember failed, member already deactivated");
            actResult.ResultType = ActivationResultType.NoChange;
            actResult.ErrorMessage = $"Member {id} is already not active";
            return actResult;
        }

        try
        {
            member.Active = false;
            member.InactiveDate = DateTime.UtcNow;
            await _memberCollection.ReplaceOneAsync(filter, member);
            actResult.ResultType = ActivationResultType.Success;
            actResult.ErrorMessage = null;
            _logger.LogInformation("DeactivateMember success, member {Username} deactivated", member.Username);
            return actResult;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DeactivateMember failed, exception occured");
            actResult.ResultType = ActivationResultType.InternalError;
            actResult.ErrorMessage = "Server failed to deactivate member";
            return actResult;
        }
    }

    public async Task<ActivationResult> ActivateMember(int id)
    {
        var actResult = new ActivationResult();
        var filter = Builders<Member>.Filter.Eq("_id", id);
        var member = await _memberCollection.Find(filter).FirstOrDefaultAsync();
        if (member == null)
        {
            actResult.ResultType = ActivationResultType.NotFound;
            actResult.ErrorMessage = $"Member {id} not found";
            return actResult;
        }

        if (member.Active)
        {
            actResult.ResultType = ActivationResultType.NoChange;
            actResult.ErrorMessage = $"Member {id} is already active";
            return actResult;
        }

        try
        {
            member.Active = true;
            member.InactiveDate = null;
            await _memberCollection.ReplaceOneAsync(filter, member);
            actResult.ResultType = ActivationResultType.Success;
            actResult.ErrorMessage = null;
            return actResult;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "DeactivateMember failed");
            actResult.ResultType = ActivationResultType.InternalError;
            actResult.ErrorMessage = "Server failed to deactivate member";
            return actResult;
        }
    }

    private async Task<Member?> GetMemberByUsername(string username)
    {
        var filter = Builders<Member>.Filter.Eq(x => x.Username, username);
        var result = await _memberCollection.Find(filter).FirstOrDefaultAsync();
        return result;
    }
    
    private async Task<int> GetMaxId()
    {
        _logger.LogDebug("GetMaxId called from MemberRepositoryMongoDb");
        
        try
        {
            var filter = Builders<Member>.Filter.Empty;
            var sort = Builders<Member>.Sort.Descending("_id");
        
            var result = await _memberCollection.Find(filter).Sort(sort).Limit(1).FirstOrDefaultAsync();
            var maxId = result?.MemberId ?? 0;

            _logger.LogDebug(
                "GetMaxId returned {MaxId} from Members collection",
                maxId);

            return maxId;
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "GetMaxId failed from Members collection");

            return 0;
        }
    }
    
}