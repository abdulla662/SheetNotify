# SheetNotify

A .NET 8 BackgroundService that **watches a Google Sheet for new bookings** and automatically sends a WhatsApp confirmation message to each customer via Twilio — with rate limiting, retry logic, and restart-safe state tracking.

[![.NET](https://img.shields.io/badge/.NET_8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![Google Sheets](https://img.shields.io/badge/Google_Sheets_API-34A853?style=for-the-badge&logo=google-sheets&logoColor=white)](https://developers.google.com/sheets/api)
[![Twilio](https://img.shields.io/badge/Twilio_WhatsApp-F22F46?style=for-the-badge&logo=twilio&logoColor=white)](https://www.twilio.com/whatsapp)
[![Docker](https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://docker.com)

---

## The Engineering Problem

A small business logs bookings in Google Sheets. Manually sending WhatsApp confirmations is slow and error-prone. This service automates it — but several non-trivial problems must be solved:

- **Rate limiting** — Twilio's WhatsApp API has a rate limit. The service waits 1.1 seconds between each message to stay safely under it.
- **Restart safety** — If the service crashes after sending row 5 but before row 10, it must not re-send rows 1–5 on restart. A `StateTracker` persists the last processed row index to disk.
- **Failure isolation** — If a WhatsApp send fails (network error, invalid number), the service stops the current cycle and retries from that row on the next poll, not from the beginning.
- **Third-party API authentication** — Google Sheets uses OAuth2 service accounts; the credential file is mounted at runtime, never hardcoded.
- **Polling vs webhooks** — Google Sheets has no native webhook support. The service polls every 30 seconds (configurable), reads only new rows (rows after the last processed index), and processes them.

---

## Google Sheet Format

Your sheet should have these columns in row 1 (headers), data from row 2:

| A | B | C | D | E | F |
|---|---|---|---|---|---|
| CustomerName | PhoneNumber | ServiceType | BookingDate | TimeSlot | Notes |

---

## Setup

### 1. Google Sheets API — Service Account
1. Go to [Google Cloud Console](https://console.cloud.google.com/) → APIs & Services → Enable **Google Sheets API**
2. Create a **Service Account** → download the JSON key as `service-account.json`
3. Share your Google Sheet with the service account email (Viewer role)

### 2. Twilio WhatsApp
1. Sign up at [twilio.com](https://www.twilio.com)
2. Activate the WhatsApp Sandbox at [console.twilio.com/messaging/whatsapp](https://console.twilio.com/us1/develop/sms/try-it-out/whatsapp-learn)
3. Get your Account SID, Auth Token, and sandbox number (`whatsapp:+14155238886`)

### 3. Configure `appsettings.json`
```json
{
  "PollIntervalSeconds": 30,
  "GoogleSheets": {
    "SpreadsheetId": "YOUR_SPREADSHEET_ID",
    "Range": "Sheet1!A2:F"
  },
  "Twilio": {
    "AccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "AuthToken": "your_auth_token",
    "FromNumber": "whatsapp:+14155238886",
    "BusinessName": "Your Business Name"
  }
}
```

### 4. Run
```bash
# Place service-account.json in the project root, then:
dotnet run --project SheetNotify
```

### Docker
```bash
docker build -t sheetnotify .
docker run \
  -v /path/to/service-account.json:/app/service-account.json \
  -v /path/to/state:/app \
  sheetnotify
```

---

## Message Template

```
✅ Booking Confirmed!

Hello Ahmed Hassan, your booking has been confirmed.

📋 Service: Haircut
📅 Date: 2024-03-15
🕐 Time: 10:00 AM
📝 Notes: Prefers trimming only

Thank you for choosing Our Salon.
Reply to this message if you need to reschedule.
```

---

## Architecture

```
SheetNotify/
├── Models/
│   └── BookingRow.cs           # Immutable record from the sheet
├── Services/
│   ├── SheetsReader.cs         # Google Sheets API client (service account auth)
│   ├── WhatsAppSender.cs       # Twilio WhatsApp sender with message builder
│   └── StateTracker.cs         # Persists last processed row to disk
└── Workers/
    └── BookingWatcher.cs       # BackgroundService — polls, processes, rate-limits
```

**Key engineering decisions:**

| Problem | Solution |
|---------|----------|
| Rate limiting | 1.1s delay between sends; stops cycle on first failure |
| Restart safety | Row index saved to file after each successful send |
| Skip invalid rows | Rows with no phone number are skipped, row index still advances |
| Auth | Service account JSON mounted at runtime, never in repo |
| Polling interval | Configurable via `PollIntervalSeconds` — no code change needed |

---

Built by [Abdulla Hamdy](https://github.com/abdulla662)
