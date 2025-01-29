using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Get Base URL from Ocelot Configuration
var baseUrl = builder.Configuration["GlobalConfiguration:BaseUrl"];
if (!string.IsNullOrEmpty(baseUrl))
{
    builder.WebHost.UseUrls(baseUrl);
    Console.WriteLine($" API Gateway running on: {baseUrl}");
}

//  Ensure the Admin Secret Key is present
var adminSecretKey = builder.Configuration["Jwt:SecretKey"];
if (string.IsNullOrEmpty(adminSecretKey))
{
    throw new Exception(" ERROR: Admin JWT Secret Key is missing in appsettings.json!");
}

//  Configure Authentication for Firebase & JWT Admin
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer("Firebase", options =>
{
    var firebaseProjectId = "doctor-appointment-syste-42850";
    options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
        ValidateAudience = true,
        ValidAudience = firebaseProjectId,
        ValidateLifetime = true
    };
})
.AddJwtBearer("Admin", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(adminSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = "your-issuer", // Update with your actual Admin issuer
        ValidateAudience = true,
        ValidAudience = "your-audience", // Update with your actual Admin audience
        ValidateLifetime = true
    };
});

//  Enable Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireAuthenticatedUser().RequireRole("Admin"));
});

//  Enable Swagger Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//  Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

//  Add Ocelot
builder.Services.AddOcelot();

var app = builder.Build();

//  Ensure Swagger is loaded before Ocelot
if (app.Environment.IsDevelopment())
{
}




app.UseSwagger();
app.UseSwaggerUI(options => {
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Doctor Appointment Gateway");
    options.RoutePrefix = string.Empty;
});
//}

// Correct Middleware Order
app.UseCors("AllowAll");
app.UseAuthentication();  //  Authentication should be BEFORE Ocelot
app.UseAuthorization();   //  Authorization should be BEFORE Ocelot
app.UseOcelot().Wait();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
