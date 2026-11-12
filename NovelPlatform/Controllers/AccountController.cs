using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovelPlatform.Data;
using NovelPlatform.Models;

namespace NovelPlatform.Controllers;

public class AccountController(AppDbContext db, ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        string username,
        string password,
        string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > 180 || password is not { Length: > 0 and <= 128 })
        {
            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu chưa đúng.");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        var user = await db.Users
            .Include(item => item.Role)
            .FirstOrDefaultAsync(item =>
                item.Username == username || item.Email == username);

        if (user is null
            || user.Status != "Active"
            || !SeedData.Verify(password, user.PasswordHash))
        {
            logger.LogWarning("Login rejected");
            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu chưa đúng.");
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        await SignIn(user);
        logger.LogInformation("User {UserId} signed in", user.Id);
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string username,
        string email,
        string displayName,
        string password)
    {
        username = username?.Trim() ?? "";
        email = email?.Trim() ?? "";
        displayName = displayName?.Trim() ?? "";

        if (username.Length < 3
            || username.Length > 50
            || password is not { Length: >= 8 and <= 128 }
            || displayName.Length is < 1 or > 80
            || email.Length > 180
            || !new EmailAddressAttribute().IsValid(email)
            || email.Length == 0)
        {
            ModelState.AddModelError("", "Vui lòng nhập tên đăng nhập từ 3 ký tự, email hợp lệ, tên hiển thị và mật khẩu từ 8 ký tự.");
            return View();
        }

        if (await db.Users.AnyAsync(user =>
                user.Username == username || user.Email == email))
        {
            ModelState.AddModelError("", "Tên đăng nhập hoặc email đã được sử dụng.");
            return View();
        }

        var readerRole = await db.Roles.FirstOrDefaultAsync(role => role.Name == "Reader");
        if (readerRole is null)
        {
            ModelState.AddModelError("", "Hệ thống chưa khởi tạo vai trò người đọc.");
            return View();
        }

        var user = new AppUser
        {
            Username = username,
            Email = email,
            DisplayName = displayName,
            PasswordHash = SeedData.Hash(password),
            RoleId = readerRole.Id,
            Status = "Active"
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        user.Role = readerRole;
        await SignIn(user);
        logger.LogInformation("Reader account {UserId} created", user.Id);

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private Task SignIn(AppUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Role, user.Role.Name),
            new Claim("session_stamp", SeedData.SessionStamp(user.PasswordHash)),
            new Claim("session_version", user.SessionVersion.ToString()),
            new Claim("uid", user.Id.ToString())
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal);
    }
}
