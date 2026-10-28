using System.ComponentModel.DataAnnotations;

namespace CyberSecurityApp.ViewModels;

public class CreateUserViewModel
{
    [Required(ErrorMessage = "Wybierz rolę użytkownika.")]
    public string Role { get; set; } = "Czytelnik";
}