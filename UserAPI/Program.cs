using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using NLog;
using NLog.Web;
using Scalar.AspNetCore;
using UserAPI.Repositories;
using UserAPI.Repositories.Interfaces;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

var logger = LogManager.Setup().LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

logger.Debug("Starting UserService");

// Endpoint til vault, vault og Service skal være på samme docker netværk, så 'localhost' bliver til 'vault' i endpoint
var EndPoint = Environment.GetEnvironmentVariable("VAULT_URL") ?? "https://localhost:8201/";
logger.Debug("Connecting to Hashicorp Vault on: {0}", EndPoint);
var httpClientHandler = new HttpClientHandler();
httpClientHandler.ServerCertificateCustomValidationCallback =
    (message, cert, chain, sslPolicyErrors) => { return true; };
    
// Initialize one of the several auth methods.
IAuthMethodInfo authMethod =
    new TokenAuthMethodInfo("00000000-0000-0000-0000-000000000000");
// Initialize settings. You can also set proxies, custom delegates etc. here.
var vaultClientSettings = new VaultClientSettings(EndPoint, authMethod)
{
    Namespace = "",
    MyHttpClientProviderFunc = handler
        => new HttpClient(httpClientHandler) {
            BaseAddress = new Uri(EndPoint)
        }
};
logger.Debug("Getting JWT secret, DB connectionstring and database name from Vault");
IVaultClient vaultClient = new VaultClient(vaultClientSettings);
try
{
    Secret<SecretData> jwtSecret = await vaultClient.V1.Secrets.KeyValue.V2
        .ReadSecretAsync(path: "auth", mountPoint: "secret");
    string jwtSecretString = jwtSecret.Data.Data["JWT_SECRET"].ToString();
    if (string.IsNullOrWhiteSpace(jwtSecretString))
        throw new NullReferenceException("JWT_SECRET not found");
    Console.WriteLine(jwtSecretString);
    Environment.SetEnvironmentVariable("JWT_SECRET", jwtSecretString);
    
    Secret<SecretData> mongoSecrets = await vaultClient.V1.Secrets.KeyValue.V2
        .ReadSecretAsync(path: "mongo", mountPoint: "secret");
    
    string connectionString;
    if (Environment.GetEnvironmentVariable("DOCKER") != null)
    {
        connectionString = mongoSecrets
                               .Data.Data["MONGO_CONNECTION_STRING"]?.ToString()
                           ?? throw new NullReferenceException(
                               "MONGO_CONNECTION_STRING not found in Vault");
    }
    else
    {
        connectionString = "mongodb://admin:secret123@localhost:27017/?authSource=admin";
    }
    Console.WriteLine(connectionString);
    Environment.SetEnvironmentVariable("MONGO_CONNECTION_STRING", connectionString);
    
    string mongoDbName = mongoSecrets.Data.Data["MONGO_USER_DB"].ToString();
    if (string.IsNullOrWhiteSpace(mongoDbName))
        throw new NullReferenceException("MONGO_DATABASE_NAME not found");
    Console.WriteLine(mongoDbName);
    Environment.SetEnvironmentVariable("MONGO_DATABASE_NAME", mongoDbName);
    
}
catch (Exception e)
{
    logger.Error($"{e.InnerException.Message}");
    Console.WriteLine("Something went wrong connecting to Vault: " + e.InnerException.Message);
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient("authService", client =>
{
    var authServiceUrl = Environment.GetEnvironmentVariable("AUTHSERVICE_URL"); 
    if (string.IsNullOrWhiteSpace(authServiceUrl))
        Console.WriteLine("Environment variable AUTHSERVICE_URL is not set, using localhost variable instead");
    client.BaseAddress = new Uri(authServiceUrl ?? "http://localhost:5028/");
});

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.RequireHttpsMetadata = false;
        o.TokenValidationParameters = new TokenValidationParameters()
        {
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET"))),
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");

    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("MONGO_CONNECTION_STRING environment variable is not set");
    return new MongoClient(connectionString);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var mongoClient = sp.GetRequiredService<IMongoClient>();
    
    var databaseName = Environment.GetEnvironmentVariable("MONGO_DATABASE_NAME");
    
    if (string.IsNullOrWhiteSpace(databaseName))
        throw new InvalidOperationException("MONGO_DATABASE_NAME environment variable is not set");
    
    return mongoClient.GetDatabase(databaseName);
});

builder.Services.AddScoped<IAdminRepository, AdminRepositoryMongoDb>();
builder.Services.AddScoped<IMemberRepository, MemberRepositoryMongoDb>();
builder.Services.AddScoped<IPersonalTrainerRepository, PersonalTrainerRepositoryMongoDb>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor",
        policy =>
        {
            policy.AllowAnyHeader()
                .AllowAnyMethod()
                .AllowAnyOrigin();
        });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor");
app.UseAuthorization();

app.MapControllers();

app.Run();
