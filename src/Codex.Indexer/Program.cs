using Codex.Indexer;
using Codex.Indexer.Configuration;
using Codex.Indexer.Data;
using Codex.Indexer.Indexing;
using Npgsql;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "O";
});

var docsRoot = builder.Configuration["Codex:DocsRoot"];
if (string.IsNullOrWhiteSpace(docsRoot))
{
    // Keep temporary compatibility with existing env keys used in compose.
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

// Poll interval is configurable to tune local behavior without code changes.
var pollIntervalSeconds = builder.Configuration.GetValue("Codex:Indexer:PollIntervalSeconds", 2);
if (pollIntervalSeconds < 1)
{
    throw new InvalidOperationException("Codex:Indexer:PollIntervalSeconds must be at least 1.");
}

var sourceName = ResolveSourceName(builder.Configuration, docsRoot);

builder.Services.AddSingleton(new CodexSettings(docsRoot, sourceName, pollIntervalSeconds));
// Reuse one pool-backed datasource for the process lifetime.
builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
builder.Services.AddSingleton<MarkdownDocumentScanner>();
builder.Services.AddScoped<DocumentsStore>();
builder.Services.AddScoped<IndexJobsStore>();
builder.Services.AddScoped<SourcesStore>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

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
