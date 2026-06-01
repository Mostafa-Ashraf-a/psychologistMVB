using Microsoft.EntityFrameworkCore;
using MentalHealthAssessment.Infrastructure.Data;
using MentalHealthAssessment.Application.Interfaces;
using MentalHealthAssessment.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Register the Swagger generator, defining 1 or more Swagger documents
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Mental Health Assessment API",
        Version = "v1",
        Description = "API for the Mental Health Assessment System in KSA"
    });
});

// Configure PostgreSQL DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("MentalHealthAssessment.Api")));

// Register Firestore Service
builder.Services.AddSingleton<IFirestoreService, FirestoreService>();

var app = builder.Build();

// Enable Swagger UI in both development and production for easy API viewing by the user
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Mental Health Assessment API v1");
    c.RoutePrefix = string.Empty; // Serve Swagger UI at the application's root (e.g. http://localhost:5000/)
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
