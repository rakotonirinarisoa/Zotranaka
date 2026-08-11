using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Configuration;
using MoraTuk.API.Data;
using MoraTuk.API.Hubs;
using MoraTuk.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// SignalR
builder.Services.AddSignalR();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Services
builder.Services.AddScoped<DistanceService>();

// MVola
builder.Services.Configure<MvolaSettings>(
    builder.Configuration.GetSection("Mvola"));

builder.Services.AddHttpClient<IMvolaService, MvolaService>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS désactivé pour le moment
// app.UseHttpsRedirection();

app.UseAuthorization();

// Controllers
app.MapControllers();

// SignalR
app.MapHub<TrackingHub>("/trackingHub");

app.Run();