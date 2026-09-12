using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using System.Data;

namespace AspireApp1.StateStore;

/// <summary>
/// Handles database schema creation and evolution.
/// Calls EnsureCreatedAsync for a fresh database and then adds any new tables
/// that may be missing from existing databases (schema evolution without migrations).
/// </summary>
public static class DatabaseInitializer
{
    public static async Task EnsureSchemaAsync(StateStoreDbContext db, CancellationToken cancellationToken = default)
    {
        // Creates the full schema if the database is new; no-op if it already exists
        await db.Database.EnsureCreatedAsync(cancellationToken);

        // Idempotent DDL for tables added in later iterations — only supported by relational providers.
        // Skipped when using the EF Core in-memory provider (e.g. in unit tests).
        if (!db.Database.IsRelational())
        {
            return;
        }

        if (!db.Database.IsSqlite())
        {
            return;
        }

        // SQLite-specific idempotent DDL for tables added in later iterations — safe to run against
        // both fresh databases (tables already created above) and existing databases
        // that were created before these tables were added to the EF Core model.
        // NOTE: Datetime columns are TEXT to match EF Core's SQLite convention,
        // which stores DateTimeOffset values as ISO 8601 strings.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "FlowRunRecords" (
                "Id"            INTEGER PRIMARY KEY AUTOINCREMENT,
                "FlowRunId"     TEXT NOT NULL,
                "FlowName"      TEXT NOT NULL,
                "CorrelationId" TEXT NOT NULL,
                "TraceId"       TEXT,
                "StartedAt"     TEXT NOT NULL,
                "CompletedAt"   TEXT,
                "Status"        TEXT NOT NULL,
                "ErrorMessage"  TEXT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_FlowRunRecords_FlowRunId"
                ON "FlowRunRecords" ("FlowRunId");
            CREATE INDEX IF NOT EXISTS "IX_FlowRunRecords_CorrelationId"
                ON "FlowRunRecords" ("CorrelationId");
            CREATE INDEX IF NOT EXISTS "IX_FlowRunRecords_TraceId"
                ON "FlowRunRecords" ("TraceId");

            CREATE TABLE IF NOT EXISTS "FlowStepRecords" (
                "Id"           INTEGER PRIMARY KEY AUTOINCREMENT,
                "FlowRunId"    TEXT NOT NULL,
                "StepName"     TEXT NOT NULL,
                "ServiceName"  TEXT NOT NULL,
                "StepOrder"    INTEGER NOT NULL,
                "Status"       TEXT NOT NULL,
                "StartedAt"    TEXT,
                "CompletedAt"  TEXT,
                "ErrorMessage" TEXT,
                "TraceId"      TEXT,
                "SpanId"       TEXT,
                "RetryAttempt" INTEGER NOT NULL DEFAULT 0,
                "MaxRetries"   INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS "IX_FlowStepRecords_FlowRunId"
                ON "FlowStepRecords" ("FlowRunId");
            CREATE INDEX IF NOT EXISTS "IX_FlowStepRecords_TraceId"
                ON "FlowStepRecords" ("TraceId");

            CREATE TABLE IF NOT EXISTS "SpanRecords" (
                "Id"            INTEGER PRIMARY KEY AUTOINCREMENT,
                "TraceId"       TEXT NOT NULL,
                "SpanId"        TEXT NOT NULL,
                "ParentSpanId"  TEXT,
                "ServiceName"   TEXT NOT NULL,
                "OperationName" TEXT NOT NULL,
                "StartTime"     TEXT NOT NULL,
                "EndTime"       TEXT,
                "Status"        TEXT NOT NULL,
                "ErrorMessage"  TEXT,
                "HttpStatusCode" INTEGER,
                "CreatedAt"     TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_SpanRecords_TraceId"
                ON "SpanRecords" ("TraceId");
            """, cancellationToken);

        await AddColumnIfMissingAsync(
            db,
            tableName: "FlowStepRecords",
            columnName: "RetryAttempt",
            alterSql: "ALTER TABLE \"FlowStepRecords\" ADD COLUMN \"RetryAttempt\" INTEGER NOT NULL DEFAULT 0;",
            cancellationToken);

        await AddColumnIfMissingAsync(
            db,
            tableName: "FlowStepRecords",
            columnName: "MaxRetries",
            alterSql: "ALTER TABLE \"FlowStepRecords\" ADD COLUMN \"MaxRetries\" INTEGER NOT NULL DEFAULT 0;",
            cancellationToken);
    }

    private static async Task AddColumnIfMissingAsync(
        StateStoreDbContext db,
        string tableName,
        string columnName,
        string alterSql,
        CancellationToken cancellationToken)
    {
        if (await ColumnExistsAsync(db, tableName, columnName, cancellationToken))
        {
            return;
        }

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync(alterSql, cancellationToken);
                return;
            }
            catch (SqliteException ex) when (
                ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            catch (SqliteException ex) when (
                ex.SqliteErrorCode == 5 || // SQLITE_BUSY
                ex.SqliteErrorCode == 6)   // SQLITE_LOCKED
            {
                if (attempt == maxAttempts)
                {
                    throw;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
            }
        }
    }

    private static async Task<bool> ColumnExistsAsync(
        StateStoreDbContext db,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.Equals(reader["name"]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            if (openedHere)
            {
                await connection.CloseAsync();
            }
        }
    }
}
