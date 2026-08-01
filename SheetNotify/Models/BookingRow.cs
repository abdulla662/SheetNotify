namespace SheetNotify.Models;

public record BookingRow(
    int RowIndex,
    string CustomerName,
    string PhoneNumber,
    string ServiceType,
    string BookingDate,
    string TimeSlot,
    string Notes
);
