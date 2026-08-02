using Microsoft.EntityFrameworkCore;
using MoraTuk.API.Data;
using MoraTuk.API.Hubs;
using MoraTuk.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddScoped<DistanceService>();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Désactivé pour le moment
// app.UseHttpsRedirection();

app.UseAuthorization();


app.MapControllers();
app.MapHub<TrackingHub>("/trackingHub");

app.Run();