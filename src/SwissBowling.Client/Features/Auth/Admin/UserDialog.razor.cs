using Microsoft.AspNetCore.Components;
using SwissBowling.Client.Shared.Services;
using SwissBowling.Core.Auth;

namespace SwissBowling.Client.Features.Auth.Admin;

public partial class UserDialog
{
    /// <summary>Gesetzt: nur Passwort ändern. Nicht gesetzt: neuen Benutzer anlegen.</summary>
    [Parameter] public UserDto? User { get; set; }

    private string _email = string.Empty;
    private string _password = string.Empty;
    private bool _isAdmin;
    private bool _isScorer = true;

    private static string? ValidatePassword(string value) =>
        value.Length < 8 ? "Mindestens 8 Zeichen"
        : !value.Any(char.IsUpper) || !value.Any(char.IsLower) || !value.Any(char.IsDigit) ? "Gross-, Kleinbuchstaben und Ziffer erforderlich"
        : null;

    private Task SaveAsync() => SubmitAsync(
        async () =>
        {
            if (User is { } user)
            {
                await Api.PutAsync<Unit>(AuthRoutes.Password(user.Id), new SetPasswordRequest(_password));
                return user;
            }

            var roles = new List<string>();
            if (_isAdmin)
            {
                roles.Add(Roles.Admin);
            }

            if (_isScorer)
            {
                roles.Add(Roles.Scorer);
            }

            return await Api.PostAsync<UserDto>(AuthRoutes.Users, new CreateUserRequest(_email.Trim(), _password, roles));
        },
        User is null ? "Benutzer angelegt." : "Passwort gesetzt.");
}
