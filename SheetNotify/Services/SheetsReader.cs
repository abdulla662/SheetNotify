using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Options;
using SheetNotify.Models;

namespace SheetNotify.Services;

public class SheetOptions
{
    public string SpreadsheetId { get; set; } = string.Empty;
    public string Range { get; set; } = "Sheet1!A2:G";
    public string ServiceAccountKeyPath { get; set; } = "service-account.json";
}

public class SheetsReader(IOptions<SheetOptions> opts, ILogger<SheetsReader> logger)
{
    private SheetsService? _service;

    private SheetsService GetService()
    {
        if (_service is not null) return _service;

        GoogleCredential credential;
        if (File.Exists(opts.Value.ServiceAccountKeyPath))
        {
            using var stream = File.OpenRead(opts.Value.ServiceAccountKeyPath);
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);
        }
        else
        {
            // Fall back to Application Default Credentials (Cloud Run / GKE)
            credential = GoogleCredential.GetApplicationDefault()
                .CreateScoped(SheetsService.Scope.SpreadsheetsReadonly);
        }

        _service = new SheetsService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName       = "SheetNotify",
        });

        return _service;
    }

    public async Task<List<BookingRow>> GetNewRowsAsync(int fromRow, CancellationToken ct)
    {
        try
        {
            var svc      = GetService();
            var range    = opts.Value.Range;
            var request  = svc.Spreadsheets.Values.Get(opts.Value.SpreadsheetId, range);
            var response = await request.ExecuteAsync(ct);

            var rows   = response.Values ?? [];
            var result = new List<BookingRow>();

            for (int i = 0; i < rows.Count; i++)
            {
                int absoluteRow = i + 2; // +2 because data starts at row 2 (row 1 = header)
                if (absoluteRow <= fromRow) continue;

                var row = rows[i];
                string Get(int idx) => idx < row.Count ? row[idx]?.ToString()?.Trim() ?? "" : "";

                result.Add(new BookingRow(
                    RowIndex:     absoluteRow,
                    CustomerName: Get(0),
                    PhoneNumber:  Get(1),
                    ServiceType:  Get(2),
                    BookingDate:  Get(3),
                    TimeSlot:     Get(4),
                    Notes:        Get(5)
                ));
            }

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read Google Sheets data.");
            return [];
        }
    }
}
