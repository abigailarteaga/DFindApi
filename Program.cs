using DFindApi.Data;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DFind API",
        Version = "v1"
    });
});

// Repos
builder.Services.AddScoped<AuthRepository>();

var app = builder.Build();

// 👇 QUITA el if (solo desarrollo) y deja Swagger SIEMPRE
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DFind API v1");
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
