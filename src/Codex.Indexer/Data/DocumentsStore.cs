using Codex.Indexer.Indexing;
using Npgsql;
using NpgsqlTypes;

namespace Codex.Indexer.Data;

public sealed record DocumentSyncResult(
    int ScannedCount,
    int UpsertedCount,
    int DeletedCount);

public sealed class DocumentsStore(NpgsqlDataSource dataSource)
{
    // Update existing rows only when checksum changes to keep writes idempotent.
    private const string UpsertDocumentSql = """
        INSERT INTO documents (path, title, content, checksum)
        VALUES (@path, @title, @content, @checksum)
        ON CONFLICT (path) DO UPDATE
        SET title = EXCLUDED.title,
            content = EXCLUDED.content,
            checksum = EXCLUDED.checksum
        WHERE documents.checksum IS DISTINCT FROM EXCLUDED.checksum;
        """;

    // Empty path array intentionally deletes all rows to mirror an empty docs root scan.
    private const string DeleteMissingDocumentsSql = """
        DELETE FROM documents
        WHERE NOT (path = ANY(@paths));
        """;

    public async Task<DocumentSyncResult> SyncDocumentsAsync(
        IReadOnlyList<ScannedMarkdownDocument> scannedDocuments,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        // Keep file-to-db sync atomic so every scan commits as one deterministic state.
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var upsertedCount = 0;
        foreach (var scannedDocument in scannedDocuments)
        {
            await using var upsertCommand =
                new NpgsqlCommand(UpsertDocumentSql, connection, transaction);
            upsertCommand.Parameters.AddWithValue("path", scannedDocument.Path);
            upsertCommand.Parameters.AddWithValue("title", scannedDocument.Title);
            upsertCommand.Parameters.AddWithValue("content", scannedDocument.Content);
            upsertCommand.Parameters.AddWithValue("checksum", scannedDocument.Checksum);
            upsertedCount += await upsertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        var allDocumentPaths = scannedDocuments.Select(document => document.Path).ToArray();
        await using var deleteCommand =
            new NpgsqlCommand(DeleteMissingDocumentsSql, connection, transaction);
        deleteCommand.Parameters.Add(
            new NpgsqlParameter<string[]>("paths", allDocumentPaths)
            {
                NpgsqlDbType = NpgsqlDbType.Array | NpgsqlDbType.Text
            });
        var deletedCount = await deleteCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return new DocumentSyncResult(scannedDocuments.Count, upsertedCount, deletedCount);
    }
}
