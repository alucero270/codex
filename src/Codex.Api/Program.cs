using Codex.Api.Configuration;
using Codex.Api.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Root path is configuration-only. The API never accepts it from clients.
var docsRoot = builder.Configuration["Codex:DocsRoot"];
if (string.IsNullOrWhiteSpace(docsRoot))
{
    docsRoot = builder.Configuration["Codex:DocsRootPath"];
}

if (string.IsNullOrWhiteSpace(docsRoot))
{
    throw new InvalidOperationException("Codex:DocsRoot configuration is required.");
}

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:Default configuration is required.");
}

builder.Services.AddSingleton(new CodexSettings(docsRoot));
// Minimal data access for issue scope: parameterized SQL through Npgsql.
builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
builder.Services.AddScoped<IndexJobsStore>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
