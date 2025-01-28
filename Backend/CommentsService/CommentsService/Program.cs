
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;
using CommentsService.Models;
using CommentsService.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.AddSingleton<ReviewService>();
builder.Services.AddControllers().AddNewtonsoftJson();



var app = builder.Build();


var reviewService = app.Services.GetRequiredService<ReviewService>();
bool isMongoConnected = await reviewService.TestMongoConnection();

if (isMongoConnected)
{
    Console.WriteLine("MongoDB connection successfull!");
}
else
{
    Console.WriteLine("MongoDB connection failed! Please check the connection...");
}


// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
