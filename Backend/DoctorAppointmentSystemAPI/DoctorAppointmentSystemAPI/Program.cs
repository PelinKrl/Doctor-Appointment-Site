using DoctorAppointmentSystemAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FirebaseAdmin;
//using Google.Apis.Auth.OAuth0;
using MongoDB.Driver;
using MongoDB.Bson;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Initialize Firebase Admin SDK
FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile(
        builder.Configuration["Authentication:Firebase:ServiceAccountKeyPath"]),
});

// Configure MongoDB
var mongoSettings = builder.Configuration.GetSection("MongoDb");
var mongoClient = new MongoClient(mongoSettings["ConnectionString"]);
var mongoDatabase = mongoClient.GetDatabase(mongoSettings["DatabaseName"]);

// Verify MongoDB connection
try
{
    mongoDatabase.RunCommand<BsonDocument>(new BsonDocument("ping", 1));
    Console.WriteLine("MongoDB connection successful!");
}
catch (Exception ex)
{
    Console.WriteLine($"MongoDB connection failed: {ex.Message}");
}

// Configure Authentication (Firebase-only)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://securetoken.google.com/doctor-appointment-syste-42850";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://securetoken.google.com/doctor-appointment-syste-42850",
            ValidateAudience = true,
            ValidAudience = "doctor-appointment-syste-42850",
            ValidateLifetime = true
        };
    });

// Configure CORS with production domains
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://127.0.0.1:5500",    // Development
                "https://your-production-domain.com" // Production
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "DoctorApp_";
});

// In Program.cs
builder.Services.AddScoped<IPatientService>(provider =>
    new PatientService(
        provider.GetRequiredService<AppDbContext>(),
        provider.GetRequiredService<IDistributedCache>(),
        provider.GetRequiredService<IMongoDatabase>() // Pass MongoDB instance
    )
);

// Register Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddHttpContextAccessor();


var app = builder.Build();



// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    if (endpoint?.Metadata.GetMetadata<AuthorizeAttribute>() != null)
    {
        var firebaseUserId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Check if user is an approved doctor
        var isApprovedDoctor = await context.RequestServices.GetRequiredService<AppDbContext>()
            .Doctors
            .AnyAsync(d => d.FirebaseUserId == firebaseUserId && d.IsApproved);

        if (isApprovedDoctor)
            context.Items["Role"] = "Doctor";
        else
            context.Items["Role"] = "Patient";
    }
    await next();
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend"); // Apply CORS policy
// In Program.cs, before app.UseAuthentication();
app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    if (endpoint?.Metadata.GetMetadata<AuthorizeAttribute>() != null)
    {
        var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
        if (role == null)
        {
            context.Response.StatusCode = 401;
            return;
        }
        context.Items["Role"] = role;
    }
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();