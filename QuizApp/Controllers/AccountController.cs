using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuthService _authService;

    public AccountController(ApplicationDbContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var exists = await _db.Users.AnyAsync(u => u.UserName == model.UserName);
        if (exists)
        {
            ModelState.AddModelError(string.Empty, "Пользователь с таким логином уже существует.");
            return View(model);
        }

        var user = new ApplicationUser
        {
            UserName = model.UserName,
            Password = model.Password
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        await _authService.SignInAsync(user, "User");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _db.Users.SingleOrDefaultAsync(u => u.UserName == model.UserName && u.Password == model.Password);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
            return View(model);
        }

        await _authService.SignInAsync(user, "User");

        var redirectUrl = model.ReturnUrl ?? returnUrl;
        if (!string.IsNullOrEmpty(redirectUrl) && Url.IsLocalUrl(redirectUrl))
            return Redirect(redirectUrl);

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _authService.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public async Task<IActionResult> History()
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null)
            return RedirectToAction("Login");

        var attempts = await _db.Attempts
            .Include(a => a.Topic)
            .Include(a => a.User)
            .Where(a => a.UserId == userId.Value)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync();

        return View(attempts);
    }
}

