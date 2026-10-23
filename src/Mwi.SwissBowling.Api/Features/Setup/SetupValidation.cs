using System.Net.Mail;
using Mwi.SwissBowling.Contracts;

namespace Mwi.SwissBowling.Api.Features.Setup;

public static class SetupValidation
{
    public static bool Text(string? value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= maximum;
    public static bool Contact(string? email, string? phone) =>
        (string.IsNullOrWhiteSpace(email) || email.Length <= 254 && MailAddress.TryCreate(email, out var address) && address.Address == email) && (phone?.Length ?? 0) <= 50;
    public static string? Player(PlayerInput input, DateOnly today)
    {
        if (!Text(input.FirstName, 100) || !Text(input.LastName, 100)) return "Vor- und Nachname sind erforderlich (je höchstens 100 Zeichen).";
        if (!Contact(input.Email, input.Phone)) return "Bitte prüfen Sie E-Mail-Adresse und Telefonnummer.";
        if (!string.IsNullOrWhiteSpace(input.SfkvNumber) && (input.SfkvNumber.Length > 30 || !input.SfkvNumber.All(char.IsAsciiDigit))) return "Die SFKV-Nummer muss aus höchstens 30 Ziffern bestehen.";
        return input.DateOfBirth > today ? "Das Geburtsdatum darf nicht in der Zukunft liegen." : null;
    }
    public static string? Event(EventInput input)
    {
        if (!Text(input.Name, 160)) return "Der Name ist erforderlich (höchstens 160 Zeichen).";
        if (input.EndDate < input.StartDate) return "Das Enddatum liegt vor dem Startdatum.";
        return input.MaximumThrows <= 0 ? "Die Wurfzahl muss positiv sein oder leer bleiben." : null;
    }
    public static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
