using GymManagement.Api.DTOs;
using GymManagement.Api.Entities;

namespace GymManagement.Api.Services;

public interface IAuthService
{
    AppUser? ValidateUser(string email, string password);
    AuthResponse GenerateToken(AppUser user);
    bool EmailExists(string email);
    (AppUser user, Member member) RegisterMember(RegisterMemberRequest request);
}
