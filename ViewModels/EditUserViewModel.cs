using System.ComponentModel.DataAnnotations;
namespace CyberSecurityApp.ViewModels;

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Podaj login.")]
    [StringLength(256, ErrorMessage = "Login moze miec maksymalnie 256 znakow.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Wybierz role uzytkownika.")]
    public string Role { get; set; } = "Czytelnik";
}
