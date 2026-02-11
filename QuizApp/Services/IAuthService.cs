using QuizApp.Models;

namespace QuizApp.Services;

public interface IAuthService
{
    /// <summary>
    /// Выполняет вход пользователя (cookie authentication).
    /// </summary>
    Task SignInAsync(ApplicationUser user, string role);

    /// <summary>
    /// Выход из системы.
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    /// Возвращает Id текущего пользователя или null.
    /// </summary>
    int? GetCurrentUserId();
}
