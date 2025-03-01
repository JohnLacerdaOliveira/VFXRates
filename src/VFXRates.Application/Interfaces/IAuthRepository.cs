using VFXRates.Domain.Entities;

namespace VFXRates.Application.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetUserByUsernameAsync(string username);
    Task<bool> AddUserAsync(User user);
}