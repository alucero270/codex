namespace Codex.Indexer.Configuration;

// Server-side settings consumed by the indexer worker.
public sealed record CodexSettings(string DocsRoot, int PollIntervalSeconds);
