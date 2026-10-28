
using System.ComponentModel.DataAnnotations;

namespace CyberSecurityApp.ViewModels;

public class SecuritySettingsViewModel
{
    [Required(ErrorMessage = "Podaj minimalną długość hasła.")]
    [Range(15, 128, ErrorMessage = "Długość hasła musi wynosić od 15 do 128 znaków.")]
    public int MinimumPasswordLength { get; set; } = 15;

    public bool EnforceSequencePolicy { get; set; }

    [Required(ErrorMessage = "Podaj liczbę dni ważności hasła.")]
    [Range(1, 3650, ErrorMessage = "Ważność hasła musi wynosić od 1 do 3650 dni.")]
    public int PasswordExpirationDays { get; set; } = 90;

    [Required(ErrorMessage = "Podaj czas bezczynności sesji.")]
    [Range(1, 1440, ErrorMessage = "Czas bezczynności musi wynosić od 1 do 1440 minut.")]
    public int SessionIdleTimeoutMinutes { get; set; } = 20;

    [Required(ErrorMessage = "Podaj ważność hasła jednorazowego.")]
    [Range(1, 168, ErrorMessage = "Ważność hasła jednorazowego musi wynosić od 1 do 168 godzin.")]
    public int OneTimePasswordValidityHours { get; set; } = 24;
}
