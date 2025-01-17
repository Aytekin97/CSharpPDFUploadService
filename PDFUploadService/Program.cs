using DotNetEnv;
using PDFUploadService.Models;
using PDFUploadService.Services;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env file for local development
DotNetEnv.Env.Load();

// Add configuration from appsettings.json and environment variables
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables();

// Register strongly-typed settings for AwsSettings
builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection("AwsSettings"));

// Register AWS S3 service
builder.Services.AddScoped<IS3Service, S3Service>();

// Add controllers
builder.Services.AddControllers();

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
    });
});

var app = builder.Build();

// Use HTTPS redirection
app.UseHttpsRedirection();

// Use the configured CORS policy
app.UseCors("CorsPolicy");

// Map controllers
app.MapControllers();

app.Run();
