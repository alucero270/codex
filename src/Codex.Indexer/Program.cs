using Codex.Indexer;
using Codex.Indexer.Configuration;
using Codex.Indexer.Data;
using Codex.Indexer.Indexing;
using Npgsql;

var builder = Host.CreateApplicationBuilder(args);

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

builder.Services.AddSingleton(new CodexSettings(docsRoot, pollIntervalSeconds));
// Reuse one pool-backed datasource for the process lifetime.
builder.Services.AddSingleton(_ => new NpgsqlDataSourceBuilder(connectionString).Build());
builder.Services.AddSingleton<MarkdownDocumentScanner>();
builder.Services.AddScoped<DocumentsStore>();
builder.Services.AddScoped<IndexJobsStore>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
