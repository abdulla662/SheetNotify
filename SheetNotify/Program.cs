using SheetNotify.Services;
using SheetNotify.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SheetOptions>(builder.Configuration.GetSection("GoogleSheets"));
builder.Services.Configure<TwilioOptions>(builder.Configuration.GetSection("Twilio"));

builder.Services.AddSingleton<StateTracker>();
builder.Services.AddSingleton<SheetsReader>();
builder.Services.AddSingleton<WhatsAppSender>();
builder.Services.AddHostedService<BookingWatcher>();

var host = builder.Build();
host.Run();
