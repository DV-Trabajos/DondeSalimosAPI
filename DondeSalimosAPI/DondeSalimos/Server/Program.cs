using DondeSalimos.Server.Data;
using DondeSalimos.Server.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextPool<Contexto>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("db")));

// Añade esto a tu método ConfigureServices
builder.Services.AddSingleton<FirebaseService>();

// Servicios mínimos para API
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000") // React
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Swagger solo en desarrollo
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
}

var app = builder.Build();

// Swagger disponible en /swagger pero no se abre automáticamente
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Pipeline mínimo
app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();
