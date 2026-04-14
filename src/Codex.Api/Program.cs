using Codex.Api.Configuration;
using Codex.Api.Data;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "O";
});

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

var sourceName = ResolveSourceName(builder.Configuration, docsRoot);

builder.Services.AddSingleton(new CodexSettings(docsRoot, sourceName));
// Minimal data access for issue scope: parameterized SQL through Npgsql.
builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
builder.Services.AddScoped<DocumentsStore>();
builder.Services.AddScoped<IndexJobsStore>();
builder.Services.AddScoped<SearchStore>();
builder.Services.AddScoped<SourcesStore>();

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

static string ResolveSourceName(IConfiguration configuration, string docsRoot)
{
    var configuredName = configuration["Codex:SourceName"];
    if (!string.IsNullOrWhiteSpace(configuredName))
    {
        return configuredName.Trim();
    }

    var normalizedRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(docsRoot));
    var leafName = Path.GetFileName(normalizedRoot);
    return string.IsNullOrWhiteSpace(leafName) ? normalizedRoot : leafName;
}
