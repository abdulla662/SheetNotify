using Microsoft.Extensions.Options;
using SheetNotify.Models;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace SheetNotify.Services;

public class TwilioOptions
{
    public string AccountSid  { get; set; } = string.Empty;
    public string AuthToken   { get; set; } = string.Empty;
    public string FromNumber  { get; set; } = string.Empty;  // whatsapp:+14155238886
    public string BusinessName { get; set; } = "Our Business";
}

public class WhatsAppSender(IOptions<TwilioOptions> opts, ILogger<WhatsAppSender> logger)
{
    public async Task<bool> SendConfirmationAsync(BookingRow booking, CancellationToken ct)
    {
        var o = opts.Value;

        // Normalise phone — must start with whatsapp: prefix for Twilio WhatsApp
        var toNumber = booking.PhoneNumber.StartsWith("whatsapp:")
            ? booking.PhoneNumber
            : $"whatsapp:{booking.PhoneNumber}";

        var message = BuildMessage(booking, o.BusinessName);

        try
        {
            TwilioClient.Init(o.AccountSid, o.AuthToken);

            var msg = await MessageResource.CreateAsync(
                body: message,
                from: new Twilio.Types.PhoneNumber(o.FromNumber),
                to:   new Twilio.Types.PhoneNumber(toNumber)
            );

            logger.LogInformation("WhatsApp sent to {To} — SID: {Sid}", toNumber, msg.Sid);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send WhatsApp to {To}", toNumber);
            return false;
        }
    }

    private static string BuildMessage(BookingRow b, string businessName) =>
        $"""
        ✅ *Booking Confirmed!*

        Hello {b.CustomerName}, your booking has been confirmed.

        📋 *Service:* {b.ServiceType}
        📅 *Date:* {b.BookingDate}
        🕐 *Time:* {b.TimeSlot}
        {(string.IsNullOrEmpty(b.Notes) ? "" : $"📝 *Notes:* {b.Notes}\n")}
        Thank you for choosing *{businessName}*.
        Reply to this message if you need to reschedule.
        """;
}
