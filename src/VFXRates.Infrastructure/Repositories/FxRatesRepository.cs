using Microsoft.EntityFrameworkCore;
using VFXRates.Application.Interfaces;
using VFXRates.Domain.Entities;
using VFXRates.Infrastructure.Data.dbContext;

namespace VFXRates.Infrastructure.Repositories
{
    public class FxRatesRepository : IFxRatesRepository
    {
        private readonly FxRatesDbContext _context;
        private readonly ILogService _logService;

        public FxRatesRepository(FxRatesDbContext context, ILogService logService)
        {
            _context = context;
            _logService = logService;
        }

        public async Task<IEnumerable<FxRate>> GetAllAsync()
        {
            await _logService.LogDebug("Retrieving all FX rates.");
            var rates = await _context.FxRates.ToListAsync();
            await _logService.LogDebug($"Retrieved {rates.Count} FX rates.");
            return rates;
        }

        public async Task<FxRate?> GetByIdAsync(int id)
        {
            await _logService.LogDebug($"Retrieving FX rate with id {id}.");
            var rate = await _context.FxRates.FindAsync(id);
            if (rate == null)
            {
                await _logService.LogWarning($"No FX rate found with id {id}.");
            }
            return rate;
        }

        public async Task<FxRate?> GetByCurrencyPairAsync(string baseCurrency, string quoteCurrency)
        {
            await _logService.LogDebug($"Searching for FX rate for {baseCurrency}/{quoteCurrency}.");
            var rate = await _context.FxRates.FirstOrDefaultAsync(r =>
                r.BaseCurrency == baseCurrency && r.QuoteCurrency == quoteCurrency);
            if (rate == null)
            {
                await _logService.LogWarning($"No FX rate found for {baseCurrency}/{quoteCurrency}.");
            }
            return rate;
        }

        public async Task<bool> AddAsync(FxRate fxRate)
        {
            await _logService.LogDebug($"Attempting to add FX rate for {fxRate.BaseCurrency}/{fxRate.QuoteCurrency}.");
            if (await CurrencyPairExistsAsync(fxRate.BaseCurrency, fxRate.QuoteCurrency))
            {
                await _logService.LogWarning($"FX rate for {fxRate.BaseCurrency}/{fxRate.QuoteCurrency} already exists.");
                return false;
            }

            _context.FxRates.Add(fxRate);
            await _logService.LogDebug($"FX rate for {fxRate.BaseCurrency}/{fxRate.QuoteCurrency} added to context.");
            return true;
        }

        public async Task UpdateAsync(FxRate fxRate)
        {
            await _logService.LogDebug($"Marking FX rate with id {fxRate.Id} for update.");
            _context.Entry(fxRate).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                await _logService.LogInformation($"FX rate with id {fxRate.Id} updated successfully.");
            }
            catch (Exception ex)
            {
                await _logService.LogError($"Error updating FX rate with id {fxRate.Id}.", ex);
                throw;
            }
        }

        public async Task DeleteAsync(FxRate fxRate)
        {
            await _logService.LogDebug($"Marking FX rate with id {fxRate.Id} for deletion.");
            _context.FxRates.Remove(fxRate);
            try
            {
                await _context.SaveChangesAsync();
                await _logService.LogInformation($"FX rate with id {fxRate.Id} deleted successfully.");
            }
            catch (Exception ex)
            {
                await _logService.LogError($"Error occurred while deleting FX rate with id {fxRate.Id}.", ex);
                throw;
            }
        }

        public async Task<bool> CurrencyPairExistsAsync(string baseCurrency, string quoteCurrency)
        {
            bool exists = await _context.FxRates.AnyAsync(r =>
                r.BaseCurrency == baseCurrency && r.QuoteCurrency == quoteCurrency);
            await _logService.LogDebug($"Currency pair {baseCurrency}/{quoteCurrency} exists: {exists}.");
            return exists;
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                await _logService.LogDebug("Saving changes to the database.");
                await _context.SaveChangesAsync();
                await _logService.LogInformation("Database changes saved successfully.");
            }
            catch (Exception ex)
            {
                await _logService.LogError($"Error occurred while saving changes to the database.", ex);
                throw;
            }
        }
    }
}
