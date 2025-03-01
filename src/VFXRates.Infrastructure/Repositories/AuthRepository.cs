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

        if(user is null)
        {
            await _logService.LogDebug($"User with username: {username} does not exist.");
            return user;
        }

        await _logService.LogDebug($"Succssefully retrieved user with username: {username}.");
        return user;
    }

    public async Task<bool> AddUserAsync(User user)
    {
        await _logService.LogDebug($"Adding user with username: {user.Username}.");

        _context.Users.Add(user);
        try
        {
            int changes = await _context.SaveChangesAsync();
            if (changes > 0)
            {
                await _logService.LogDebug($"Successfully added user with username: {user.Username}.");
                return true;
            }
            else
            {
                await _logService.LogError($"No changes were saved for user with username: {user.Username}.");
                return false;
            }
        }
        catch (Exception ex)
        {
            await _logService.LogError($"Exception occurred while adding user with username: {user.Username}.", ex);

            throw;
        }
    }
}