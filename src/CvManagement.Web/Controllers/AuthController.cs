using CvManagement.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CvManagement.Web.Controllers;

[Route("api/auth")]
public class AuthController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AuthController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
    }

    // ==========================================
    // LOGIN (email/password)
    // ==========================================
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [FromForm] string email,
        [FromForm] string password)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: user not found for {Email}", email);
                return Redirect("/login?error=Invalid+email+or+password");
            }

            if (user.IsBlocked)
            {
                return Redirect("/login?error=Account+is+blocked");
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, password, isPersistent: true, lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed for {Email}", email);
                return Redirect("/login?error=Invalid+email+or+password");
            }

            _logger.LogInformation("User {Email} logged in", email);
            return Redirect("/");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Email}", email);
            return Redirect($"/login?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    // ==========================================
    // REGISTER
    // ==========================================
    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        [FromForm] string firstName,
        [FromForm] string lastName,
        [FromForm] string email,
        [FromForm] string password,
        [FromForm] string role)
    {
        try
        {
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
            {
                return Redirect("/register?error=Email+already+registered");
            }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,
                IsBlocked = false
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                return Redirect($"/register?error={Uri.EscapeDataString(errors)}");
            }

            var validRoles = new[] { "Candidate", "Recruiter" };
            var roleToAssign = validRoles.Contains(role) ? role : "Candidate";

            var roleResult = await _userManager.AddToRoleAsync(user, roleToAssign);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(" | ", roleResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Role assignment failed for {Email}: {Errors}", email, errors);
            }

            await _signInManager.SignInAsync(user, isPersistent: true);

            _logger.LogInformation("User {Email} registered with role {Role}", email, roleToAssign);
            return Redirect("/");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error for {Email}", email);
            return Redirect($"/register?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    // ==========================================
    // LOGOUT
    // ==========================================
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out");
        return Redirect("/login");
    }

    [HttpGet("logout-get")]
    public async Task<IActionResult> LogoutGet()
    {
        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out via GET");
        return Redirect("/login");
    }

    // ==========================================
    // EXTERNAL LOGIN (Google / Facebook)
    // ==========================================
    [HttpPost("external-login")]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin([FromForm] string provider, [FromForm] string? returnUrl = null)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet("external-login-callback")]
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        try
        {
            if (remoteError != null)
            {
                _logger.LogWarning("External login error: {Error}", remoteError);
                return Redirect($"/login?error={Uri.EscapeDataString(remoteError)}");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return Redirect("/login?error=Could+not+get+external+login+info");
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider,
                info.ProviderKey,
                isPersistent: true,
                bypassTwoFactor: true);

            if (signInResult.Succeeded)
            {
                _logger.LogInformation("User signed in via {Provider}", info.LoginProvider);
                return Redirect(returnUrl ?? "/");
            }

            var email = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Redirect("/login?error=Email+not+provided+by+" + info.LoginProvider);
            }

            var existingUser = await _userManager.FindByEmailAsync(email);

            if (existingUser != null)
            {
                await _userManager.AddLoginAsync(existingUser, info);
                await _signInManager.SignInAsync(existingUser, isPersistent: true);
                _logger.LogInformation("Linked {Provider} to existing user {Email}", info.LoginProvider, email);
                return Redirect(returnUrl ?? "/");
            }

            var firstName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.GivenName)?.Value
                ?? email.Split('@')[0];
            var lastName = info.Principal.FindFirst(System.Security.Claims.ClaimTypes.Surname)?.Value
                ?? "";

            var newUser = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName
            };

            var createResult = await _userManager.CreateAsync(newUser);

            if (createResult.Succeeded)
            {
                await _userManager.AddLoginAsync(newUser, info);
                await _userManager.AddToRoleAsync(newUser, "Candidate");
                await _signInManager.SignInAsync(newUser, isPersistent: true);

                _logger.LogInformation("New user created via {Provider}: {Email}", info.LoginProvider, email);
                return Redirect(returnUrl ?? "/");
            }
            else
            {
                var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create user: {Errors}", errors);
                return Redirect($"/login?error={Uri.EscapeDataString(errors)}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External login callback error");
            return Redirect($"/login?error={Uri.EscapeDataString(ex.Message)}");
        }
    }
}