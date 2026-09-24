using CvManagement.Domain.Entities;
using CvManagement.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CvManagement.Web.Services;

public class AuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _logger = logger;
    }

    // ========================================
    // LOGIN
    // ========================================
    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: user not found for {Email}", email);
                return (false, "Invalid email or password.");
            }

            if (user.IsBlocked)
                return (false, "Your account is blocked. Contact administrator.");

            var result = await _signInManager.PasswordSignInAsync(
                user,
                password,
                isPersistent: true,
                lockoutOnFailure: false);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed for {Email}. Reason: {Reason}", email, result.ToString());
                return (false, "Invalid email or password.");
            }

            _logger.LogInformation("User {Email} logged in successfully", email);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Email}", email);
            return (false, $"Login error: {ex.Message}");
        }
    }

    // ========================================
    // REGISTER
    // ========================================
    public async Task<(bool Success, string? Error)> RegisterAsync(
        string email,
        string password,
        string firstName,
        string lastName,
        string role)
    {
        try
        {
            // 1. Check existing user
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null)
                return (false, "An account with this email already exists.");

            // 2. Create user
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true,   // Auto-confirm for now
                IsBlocked = false
            };

            var createResult = await _userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(" ", createResult.Errors.Select(e => e.Description));
                _logger.LogWarning("User creation failed for {Email}: {Errors}", email, errors);
                return (false, errors);
            }

            // 3. Assign role (Candidate or Recruiter only)
            var validRoles = new[] { "Candidate", "Recruiter" };
            var roleToAssign = validRoles.Contains(role) ? role : "Candidate";

            // Ensure role exists
            var roleExists = await _db.Roles.AnyAsync(r => r.Name == roleToAssign);
            if (!roleExists)
            {
                _logger.LogError("Role {Role} does not exist in database", roleToAssign);
                await _userManager.DeleteAsync(user); // rollback
                return (false, $"Role '{roleToAssign}' not configured. Contact admin.");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, roleToAssign);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join(" ", roleResult.Errors.Select(e => e.Description));
                await _userManager.DeleteAsync(user); // rollback
                return (false, $"Role assignment failed: {errors}");
            }

            // 4. Create profile
            var profile = new Profile
            {
                UserId = user.Id
            };
            _db.Profiles.Add(profile);
            await _db.SaveChangesAsync();

            _logger.LogInformation("User {Email} registered with role {Role}", email, roleToAssign);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error for {Email}", email);
            return (false, $"Registration error: {ex.Message}");
        }
    }

    // ========================================
    // LOGOUT
    // ========================================
    public async Task LogoutAsync()
    {
        await _signInManager.SignOutAsync();
    }
}