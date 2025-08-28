using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
namespace PcfManager.Services;

public sealed record PriceBookVersionInfo(
        long VersionId,
        int TemplateId,
        string TemplateName,
        string ExcelFileName,
        string Label
    );

public interface IPriceBookVersionService
{
    Task<PriceBookVersionInfo> BuildVersionAsync(
        int draftId,
        int templateId,
        string createdBy,
        string? label = null,
        CancellationToken ct = default);
}

public sealed class PriceBookVersionService : IPriceBookVersionService
{
    private readonly string _connStr;

    public PriceBookVersionService(string connectionString)
    {
        _connStr = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public async Task<PriceBookVersionInfo> BuildVersionAsync(
        int draftId,
        int templateId,
        string createdBy,
        string? label = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("createdBy is required.", nameof(createdBy));

        long versionId;

        await using (var conn = new SqlConnection(_connStr))
        {
            await conn.OpenAsync(ct);

            // 1) Call freeze proc -> returns @VersionId (OUTPUT)
            await using (var cmd = new SqlCommand("dbo.Chap_PriceBook_BuildFromDraft", conn)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            })
            {
                cmd.Parameters.AddWithValue("@DraftId", draftId);
                cmd.Parameters.AddWithValue("@TemplateId", templateId);
                cmd.Parameters.AddWithValue("@CreatedBy", createdBy);
                cmd.Parameters.AddWithValue("@Label", (object?)label ?? DBNull.Value);

                var outVersion = new SqlParameter("@VersionId", SqlDbType.BigInt)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outVersion);

                await cmd.ExecuteNonQueryAsync(ct);
                versionId = (long)(outVersion.Value ?? 0L);

                if (versionId <= 0)
                    throw new InvalidOperationException("Failed to build price book version (VersionId not returned).");
            }

            // 2) Lookup template info (join Version -> Template)
            const string metaSql = @"
SELECT v.TemplateId, t.TemplateName, t.ExcelFileName, v.Label
FROM dbo.Chap_PriceBookVersion v
JOIN dbo.Chap_PriceBookTemplate t ON t.TemplateId = v.TemplateId
WHERE v.VersionId = @VersionId;";

            await using var meta = new SqlCommand(metaSql, conn);
            meta.Parameters.AddWithValue("@VersionId", versionId);

            await using var r = await meta.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct))
                throw new InvalidOperationException($"VersionId {versionId} not found after build.");

            var tplId = r.GetInt32(0);
            var tplName = r.GetString(1);
            var excel = r.GetString(2);
            var lbl = r.GetString(3);

            return new PriceBookVersionInfo(versionId, tplId, tplName, excel, lbl);
        }
    }
}
