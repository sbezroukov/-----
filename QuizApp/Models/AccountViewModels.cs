using System.ComponentModel.DataAnnotations;

namespace QuizApp.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Введите логин")]
    [Display(Name = "Логин")]
    [StringLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [Display(Name = "Пароль")]
    [StringLength(200)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введите логин")]
    [Display(Name = "Логин")]
    [StringLength(100, MinimumLength = 1)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [Display(Name = "Пароль")]
    [StringLength(200, MinimumLength = 1)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
