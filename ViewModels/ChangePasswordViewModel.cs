using System.ComponentModel.DataAnnotations;
namespace CyberSecurityApp.ViewModels;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Podaj obecne hasło.")]
    [DataType(DataType.Password)]
    [Display(Name = "Obecne hasło")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Podaj nowe hasło.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nowe hasło")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Potwierdź nowe hasło.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Podane hasła nie są identyczne.")]
    [Display(Name = "Powtórz nowe hasło")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
