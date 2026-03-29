using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GymManagement.Api.Data;
using GymManagement.Api.DTOs;
using GymManagement.Api.Entities;
using Microsoft.IdentityModel.Tokens;

namespace GymManagement.Api.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public AppUser? ValidateUser(string email, string password)
    {
        var hash = PasswordHasher.Hash(password);
        var normalizedEmail = email.Trim().ToUpper();
        var user = _context.Users.FirstOrDefault(u =>
            u.Email.ToUpper() == normalizedEmail
            && u.PasswordHash == hash);

        if (user is null)
        {
            return null;
        }

        var requiresGymAssignment = user.Role == AppRole.Admin || user.Role == AppRole.Trainer;
        if (requiresGymAssignment && !user.GymTenantId.HasValue)
        {
            return null;
        }

        return user;
    }

    public AuthResponse GenerateToken(AppUser user)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Missing Jwt:Key setting.");
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Missing Jwt:Issuer setting.");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Missing Jwt:Audience setting.");
        var expiresMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var parsed) ? parsed : 120;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        if (user.GymTenantId.HasValue)
        {
            claims.Add(new Claim("gymTenantId", user.GymTenantId.Value.ToString()));
        }
        claims.Add(new Claim("mustChangePassword", user.MustChangePassword ? "true" : "false"));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString(),
            GymTenantId = user.GymTenantId,
            MustChangePassword = user.MustChangePassword,
            ProfilePhotoDataUri = user.ProfilePhotoDataUri
        };
    }

    public bool EmailExists(string email)
    {
        var normalizedEmail = email.Trim().ToUpper();
        return _context.Users.Any(u => u.Email.ToUpper() == normalizedEmail);
    }

    public (AppUser user, Member member) RegisterMember(RegisterMemberRequest request)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            PasswordHash = PasswordHasher.Hash(request.Password),
            Role = AppRole.Member
        };

        var member = new Member
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = request.Name,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Phone = request.Phone,
            Email = request.Email,
            AddressLine = request.AddressLine,
            City = request.City,
            State = request.State,
            Pincode = request.Pincode,
            Height = request.Height,
            Weight = request.Weight,
            JoiningDate = DateTime.UtcNow.Date,
            MembershipStatus = MembershipStatus.Active
        };

        _context.Users.Add(user);
        _context.Members.Add(member);
        _context.SaveChanges();
        return (user, member);
    }
}
