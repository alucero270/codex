using System.Security.Cryptography;
using System.Text;

namespace Codex.Indexer.Indexing;

public sealed record ScannedMarkdownDocument(
    string Path,
    string Title,
    string Content,
    string Checksum);

public sealed class MarkdownDocumentScanner(ILogger<MarkdownDocumentScanner> logger)
{
    public async Task<IReadOnlyList<ScannedMarkdownDocument>> ScanAsync(
        string docsRoot,
        CancellationToken cancellationToken)
    {
        var normalizedDocsRoot = NormalizeRootPath(docsRoot);

        // Stable ordering keeps repeated scans deterministic.
        var markdownFiles = EnumerateMarkdownFilesWithinBoundary(
                normalizedDocsRoot,
                cancellationToken)
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .ToArray();

        var scannedDocuments = new List<ScannedMarkdownDocument>(markdownFiles.Length);
        foreach (var markdownFile in markdownFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Read once so stored content and checksum come from the same bytes.
            var contentBytes =
                await File.ReadAllBytesAsync(markdownFile.FullPath, cancellationToken);
            var content = Encoding.UTF8.GetString(contentBytes);
            var checksum = ComputeChecksum(contentBytes);
            var title = ExtractTitle(content, markdownFile.FullPath);

            scannedDocuments.Add(
                new ScannedMarkdownDocument(
                    markdownFile.RelativePath,
                    title,
                    content,
                    checksum));
        }

        return scannedDocuments;
    }

    private IEnumerable<(string FullPath, string RelativePath)> EnumerateMarkdownFilesWithinBoundary(
        string docsRoot,
        CancellationToken cancellationToken)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(docsRoot);

        while (pendingDirectories.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentDirectory = pendingDirectories.Pop();
            IEnumerable<string> entries;
            try
            {
                entries = Directory.EnumerateFileSystemEntries(currentDirectory);
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(
                    ex,
                    "Skipped directory during ingestion because it could not be enumerated (directory_path: {DirectoryPath})",
                    currentDirectory);
                continue;
            }

            foreach (var entryPath in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!TryResolveSafeEntry(docsRoot, entryPath, out var resolvedEntry, out var isDirectory))
                {
                    continue;
                }

                if (isDirectory)
                {
                    pendingDirectories.Push(resolvedEntry);
                    continue;
                }

                if (!string.Equals(
                        Path.GetExtension(resolvedEntry),
                        ".md",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return (
                    resolvedEntry,
                    NormalizeRelativePath(Path.GetRelativePath(docsRoot, resolvedEntry)));
            }
        }
    }

    private static string ExtractTitle(string content, string fullPath)
    {
        // Prefer first heading for title; fall back to file name when no heading exists.
        using var reader = new StringReader(content);
        while (reader.ReadLine() is { } line)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Length == 0)
            {
                continue;
            }

            if (trimmedLine[0] != '#')
            {
                continue;
            }

            var title = trimmedLine.TrimStart('#').Trim();
            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }
        }

        return Path.GetFileNameWithoutExtension(fullPath);
    }

    private static string ComputeChecksum(byte[] contentBytes)
    {
        var hash = SHA256.HashData(contentBytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private bool TryResolveSafeEntry(
        string docsRoot,
        string entryPath,
        out string resolvedEntry,
        out bool isDirectory)
    {
        resolvedEntry = string.Empty;
        isDirectory = false;

        string normalizedEntryPath;
        try
        {
            normalizedEntryPath = Path.GetFullPath(entryPath);
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            logger.LogWarning(
                ex,
                "Skipped filesystem entry because its path could not be normalized safely (entry_path: {EntryPath})",
                entryPath);
            return false;
        }

        if (!IsWithinBoundary(docsRoot, normalizedEntryPath))
        {
            logger.LogWarning(
                "Skipped filesystem entry because it resolved outside the configured source boundary (entry_path: {EntryPath}, docs_root: {DocsRoot})",
                normalizedEntryPath,
                docsRoot);
            return false;
        }

        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(normalizedEntryPath);
        }
        catch (Exception ex) when (
            ex is IOException or UnauthorizedAccessException)
        {
            logger.LogWarning(
                ex,
                "Skipped filesystem entry because its attributes could not be read safely (entry_path: {EntryPath})",
                normalizedEntryPath);
            return false;
        }

        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            logger.LogWarning(
                "Skipped filesystem entry because Strata does not follow reparse points during ingestion (entry_path: {EntryPath})",
                normalizedEntryPath);
            return false;
        }

        resolvedEntry = normalizedEntryPath;
        isDirectory = (attributes & FileAttributes.Directory) != 0;
        return true;
    }

    private static string NormalizeRootPath(string docsRoot)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(docsRoot));
    }

    private static bool IsWithinBoundary(string docsRoot, string candidatePath)
    {
        var comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        if (string.Equals(candidatePath, docsRoot, comparison))
        {
            return true;
        }

        var docsRootWithSeparator = Path.EndsInDirectorySeparator(docsRoot)
            ? docsRoot
            : $"{docsRoot}{Path.DirectorySeparatorChar}";
        return candidatePath.StartsWith(docsRootWithSeparator, comparison);
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        // Normalize path separators so DB paths are consistent across OSes.
        return relativePath
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }
}
