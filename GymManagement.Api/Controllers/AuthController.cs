using GymManagement.Api.DTOs;
using GymManagement.Api.Data;
using GymManagement.Api.Entities;
using GymManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GymManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _context;

    public AuthController(IAuthService authService, AppDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpPost("register-member")]
    public IActionResult RegisterMember([FromBody] RegisterMemberRequest request)
    {
        if (_authService.EmailExists(request.Email))
        {
            return Conflict(new { message = "Email is already registered." });
        }

        var (user, _) = _authService.RegisterMember(request);
        var token = _authService.GenerateToken(user);
        return Ok(token);
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = _authService.ValidateUser(request.Email, request.Password);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var response = _authService.GenerateToken(user);
        return Ok(response);
    }

    [HttpPost("change-password")]
    [Authorize]
    public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = GetCurrentUser();
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid user session." });
        }

        var currentHash = PasswordHasher.Hash(request.CurrentPassword);
        if (!string.Equals(user.PasswordHash, currentHash, StringComparison.Ordinal))
        {
            return BadRequest(new { message = "Current password is incorrect." });
        }

        var nextPassword = request.NewPassword.Trim();
        if (nextPassword.Length < 8)
        {
            return BadRequest(new { message = "New password must be at least 8 characters." });
        }

        var nextHash = PasswordHasher.Hash(nextPassword);
        if (string.Equals(user.PasswordHash, nextHash, StringComparison.Ordinal))
        {
            return BadRequest(new { message = "New password must be different from current password." });
        }

        user.PasswordHash = nextHash;
        user.MustChangePassword = false;
        _context.SaveChanges();

        var response = _authService.GenerateToken(user);
        return Ok(response);
    }

    [HttpGet("profile")]
    [Authorize]
    public IActionResult GetProfile()
    {
        var user = GetCurrentUser();
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid user session." });
        }

        return Ok(new UserProfileResponse
        {
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            ProfilePhotoDataUri = user.ProfilePhotoDataUri
        });
    }

    [HttpPut("profile-photo")]
    [Authorize]
    public IActionResult UpdateProfilePhoto([FromBody] UpdateProfilePhotoRequest request)
    {
        var user = GetCurrentUser();
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid user session." });
        }

        var photoDataUri = request.PhotoDataUri.Trim();
        if (!photoDataUri.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only image uploads are supported." });
        }

        if (photoDataUri.Length > 2_000_000)
        {
            return BadRequest(new { message = "Image is too large. Please upload a smaller image." });
        }

        user.ProfilePhotoDataUri = photoDataUri;
        _context.SaveChanges();

        return Ok(new
        {
            message = "Profile photo updated successfully.",
            profilePhotoDataUri = user.ProfilePhotoDataUri
        });
    }

    private AppUser? GetCurrentUser()
    {
        var userIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdRaw, out var userId))
        {
            return null;
        }

        return _context.Users.FirstOrDefault(x => x.Id == userId);
    }
}
