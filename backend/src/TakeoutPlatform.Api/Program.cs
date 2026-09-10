using Microsoft.EntityFrameworkCore;
using TakeoutPlatform.Api.Common;
using TakeoutPlatform.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

const string DevCors = "dev-cors";
builder.Services.AddCors(options =>
    options.AddPolicy(DevCors, p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(DevCors);
}

app.MapControllers();

app.Run();

// 供集成测试（WebApplicationFactory）引用
public partial class Program { }
