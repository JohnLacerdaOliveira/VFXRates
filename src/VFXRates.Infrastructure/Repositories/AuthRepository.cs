using Microsoft.EntityFrameworkCore;
using VFXRates.Application.Interfaces;
using VFXRates.Domain.Entities;
using VFXRates.Infrastructure.Data.dbContext;

namespace VFXRates.Infrastructure.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly FxRatesDbContext _context;
    private readonly ILogService _logService;

    public AuthRepository(FxRatesDbContext context, ILogService logService)
    {
        _context = context;
        _logService = logService;
    }

    public async Task<User?> GetUserByUsernameAsync(string username)
    {
        await _logService.LogDebug($"Retrieving user with username: {username}.");
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
        await _logService.LogDebug($"Succssefully retrieved user with username: {username}.");

        return user;
    }

    public async Task AddUserAsync(User user)
    {
        await _logService.LogDebug($"Adding user with username: {user.Username}.");
        _context.Users.Add(user);
        await _logService.LogDebug($"Succssefully added user with username: {user.Username}.");
        await _context.SaveChangesAsync();
    }
}