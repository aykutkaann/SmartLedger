using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using SmartLedger.Domain.Interfaces;
using SmartLedger.Domain.Services;
using SmartLedger.Infrastructure.Persistence;
using SmartLedger.Infrastructure.Repositories;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' not found.");

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connStr));

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connStr));

// ── CQRS ──────────────────────────────────────────────────────────────────────


// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddSingleton<IIbanGenerator, IbanGenerator>();

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SmartLedger",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ── OpenTelemetry ─────────────────────────────────────────────────────────────


// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SmartLedger API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ── CORS (for React dev server) ───────────────────────────────────────────────
builder.Services.AddCors(opt =>
    opt.AddPolicy("ReactDev", p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ReactDev");
app.UseAuthentication();
app.UseAuthorization();


// ── Auto-migrate on startup (dev only) ───────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// ── Endpoint registration ─────────────────────────────────────────────────────


app.Run();

// Needed for integration test WebApplicationFactory
public partial class Program { }