using System.Security.Cryptography;
using System.Text;

namespace Codex.Indexer.Indexing;

public sealed record ScannedMarkdownDocument(
    string Path,
    string Title,
    string Content,
    string Checksum);

public sealed class MarkdownDocumentScanner
{
    public async Task<IReadOnlyList<ScannedMarkdownDocument>> ScanAsync(
        string docsRoot,
        CancellationToken cancellationToken)
    {
        // Stable ordering keeps repeated scans deterministic.
        var markdownFiles = Directory
            .EnumerateFiles(docsRoot, "*", SearchOption.AllDirectories)
            .Where(static filePath =>
                string.Equals(
                    Path.GetExtension(filePath),
                    ".md",
                    StringComparison.OrdinalIgnoreCase))
            .Select(filePath => new
            {
                FullPath = filePath,
                RelativePath = NormalizeRelativePath(Path.GetRelativePath(docsRoot, filePath))
            })
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

    private static string NormalizeRelativePath(string relativePath)
    {
        // Normalize path separators so DB paths are consistent across OSes.
        return relativePath
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }
}
