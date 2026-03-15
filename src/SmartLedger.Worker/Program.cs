using Microsoft.EntityFrameworkCore;
using Npgsql;
using SmartLedger.Domain.Interfaces;
using SmartLedger.Infrastructure.BackgroundJobs;
using SmartLedger.Infrastructure.Persistence;
using SmartLedger.Infrastructure.Repositories;

var builder = Host.CreateApplicationBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
var connStr = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' not found.");

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connStr));

builder.Services.AddSingleton(_ => NpgsqlDataSource.Create(connStr));

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ── Background worker ─────────────────────────────────────────────────────────
builder.Services.AddHostedService<FraudScoringWorker>();

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Logging.AddConsole();

var host = builder.Build();
await host.RunAsync();