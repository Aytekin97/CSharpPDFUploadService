using PDFUploadService.Services;
using PDFUploadService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add configuration from app.settings.json and environment variables
builder.Configuration.AddJsonFile("appsettings.json", optional: false).AddEnvironmentVariables();

// Register strongly-typed settings
builder.Services.Configure<AwsSettings>(builder.Configuration.GetSection("AwsSettings"));

// Add AWS S3 service
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

// Use HTTPS redirection if needed
app.UseHttpsRedirection();

// Use CORS policy
app.UseCors("CorsPolicy");

// Map Controllers
app.MapControllers();

app.Run();
