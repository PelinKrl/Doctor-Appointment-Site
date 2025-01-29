using DoctorAppointmentSystemAPI.Services;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Initialize Firebase Admin SDK
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(builder.Configuration["Authentication:Firebase:ServiceAccountKeyPath"]),
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

// Configure Authentication and Authorization
// Configure Authentication and Authorization
builder.Services.AddAuthentication(options => 
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var firebaseProjectId = "doctor-appointment-syste-42850";
    var adminSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]));

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        // Support multiple issuers
        ValidIssuers = new[]
        {
            $"https://securetoken.google.com/{firebaseProjectId}", // Firebase issuer
            "your-issuer"  // Your admin token issuer
        },
        // Support multiple audiences
        ValidAudiences = new[]
        {
            firebaseProjectId, // Firebase audience
            "your-audience"    // Your admin token audience
        },
        // Support multiple signing keys
        IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
        {
            // If it's a Firebase token
            if (securityToken.Issuer.StartsWith("https://securetoken.google.com"))
            {
                return GoogleSigningKeys.GetIssuerSigningKeys();
            }
            // If it's your admin token
            return new[] { adminSigningKey };
        }
    };
}); 

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// Configure CORS
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

// Register Services
builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddSingleton<IEmailService, EmailService>();
builder.Services.AddSingleton<RabbitMQService>();
builder.Services.AddHostedService<UnfinishedAppointmentProcessor>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
   
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Doctor Appointment System API v1");
    options.RoutePrefix = string.Empty;
    
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
