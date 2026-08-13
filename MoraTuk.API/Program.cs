using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Configuration;
using MoraTuk.API.Data;
using MoraTuk.API.Hubs;
using MoraTuk.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// CONTROLLERS
// ============================================================

builder.Services.AddControllers();

// ============================================================
// SIGNALR
// ============================================================

builder.Services.AddSignalR();

// ============================================================
// SWAGGER
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============================================================
// DATABASE
// ============================================================

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// ============================================================
// SERVICES
// ============================================================

builder.Services.AddScoped<DistanceService>();

// Service de gestion des payouts chauffeurs
builder.Services.AddScoped<DriverPayoutService>();

// ============================================================
// MVOLA
// ============================================================

builder.Services.Configure<MvolaSettings>(
    builder.Configuration.GetSection("Mvola"));

builder.Services.AddHttpClient<IMvolaService, MvolaService>();

// ============================================================
// APPLICATION
// ============================================================

var app = builder.Build();

// ============================================================
// SWAGGER
// ============================================================

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

// ============================================================
// HTTPS
// ============================================================

// HTTPS désactivé pour le moment
// app.UseHttpsRedirection();

app.UseAuthorization();

// ============================================================
// CONTROLLERS
// ============================================================

app.MapControllers();

// ============================================================
// SIGNALR
// ============================================================

app.MapHub<TrackingHub>("/trackingHub");

app.Run();