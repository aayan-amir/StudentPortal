using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StudentPortal.Models.ViewModels;
using StudentPortal.Services;

namespace StudentPortal.Controllers;

public class AccountController(IOptions<AdminAccountOptions> adminAccountOptions) : Controller
{
    private readonly AdminAccountOptions _adminAccount = adminAccountOptions.Value;

    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginInput { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInput input)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var usernameMatches = string.Equals(
            input.Username.Trim(),
            _adminAccount.Username,
            StringComparison.OrdinalIgnoreCase);
        var passwordMatches = SecureEquals(input.Password, _adminAccount.Password);

        if (!usernameMatches || !passwordMatches)
        {
            ModelState.AddModelError(string.Empty, "Invalid admin username or password.");
            return View(input);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, _adminAccount.Username),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return RedirectToLocal(input.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Rooms");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Pending", "Admin");
    }

    private static bool SecureEquals(string value, string expectedValue)
    {
        var valueHash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var expectedValueHash = SHA256.HashData(Encoding.UTF8.GetBytes(expectedValue));

        return CryptographicOperations.FixedTimeEquals(valueHash, expectedValueHash);
    }
}
