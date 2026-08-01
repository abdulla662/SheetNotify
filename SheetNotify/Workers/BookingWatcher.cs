using SheetNotify.Services;

namespace SheetNotify.Workers;

public class BookingWatcher(
    SheetsReader   sheetsReader,
    WhatsAppSender whatsApp,
    StateTracker   state,
    ILogger<BookingWatcher> logger,
    IConfiguration config) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromSeconds(config.GetValue<int>("PollIntervalSeconds", 30));

        logger.LogInformation("BookingWatcher started — polling every {Interval}s", pollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ProcessNewBookingsAsync(stoppingToken);
            await Task.Delay(pollInterval, stoppingToken);
        }
    }

    private async Task ProcessNewBookingsAsync(CancellationToken ct)
    {
        var lastRow = state.GetLastProcessedRow();
        logger.LogDebug("Checking for new rows after row {LastRow}", lastRow);

        var newRows = await sheetsReader.GetNewRowsAsync(lastRow, ct);

        if (newRows.Count == 0)
        {
            logger.LogDebug("No new bookings found.");
            return;
        }

        logger.LogInformation("Found {Count} new booking(s)", newRows.Count);

        int lastProcessed = lastRow;

        foreach (var booking in newRows)
        {
            if (string.IsNullOrWhiteSpace(booking.PhoneNumber))
            {
                logger.LogWarning("Row {Row} has no phone number — skipping.", booking.RowIndex);
                lastProcessed = booking.RowIndex;
                continue;
            }

            // Rate limit: Twilio WhatsApp Sandbox allows ~1 msg/sec
            await Task.Delay(1100, ct);

            var sent = await whatsApp.SendConfirmationAsync(booking, ct);

            if (sent)
            {
                logger.LogInformation("✓ Confirmation sent for row {Row} ({Name})", booking.RowIndex, booking.CustomerName);
                lastProcessed = booking.RowIndex;
            }
            else
            {
                // Stop processing on failure — don't skip rows, we'll retry next poll
                logger.LogWarning("✗ Failed at row {Row} — will retry next poll cycle.", booking.RowIndex);
                break;
            }
        }

        if (lastProcessed > lastRow)
            state.SaveLastProcessedRow(lastProcessed);
    }
}
