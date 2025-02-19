using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VFXRates.Application.Interfaces;
using VFXRates.Domain.Entities;
using VFXRates.Infrastructure.Data.dbContext;

namespace VFXRates.Infrastructure.Repositories
{
    public class FxRateRepository : IFxRateRepository
    {
        private readonly FxRatesDbContext _context;
        private readonly ILogger<FxRateRepository> _logger;

        public FxRateRepository(FxRatesDbContext context, ILogger<FxRateRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<FxRate>> GetAllAsync()
        {
            _logger.LogDebug("Retrieving all FX rates.");
            var rates = await _context.FxRates.ToListAsync();
            _logger.LogDebug("Retrieved {Count} FX rates.", rates.Count);
            return rates;
        }

        public async Task<FxRate?> GetByIdAsync(int id)
        {
            _logger.LogDebug("Retrieving FX rate with id {Id}.", id);
            var rate = await _context.FxRates.FindAsync(id);
            if (rate == null)
            {
                _logger.LogWarning("No FX rate found with id {Id}.", id);
            }
            return rate;
        }

        public async Task<FxRate?> GetByCurrencyPairAsync(string baseCurrency, string quoteCurrency)
        {
            _logger.LogDebug("Searching for FX rate for {BaseCurrency}/{QuoteCurrency}.", baseCurrency, quoteCurrency);
            var rate = await _context.FxRates.FirstOrDefaultAsync(r =>
                r.BaseCurrency == baseCurrency && r.QuoteCurrency == quoteCurrency);
            if (rate == null)
            {
                _logger.LogDebug("No FX rate found for {BaseCurrency}/{QuoteCurrency}.", baseCurrency, quoteCurrency);
            }
            return rate;
        }

        public async Task<bool> AddAsync(FxRate fxRate)
        {
            _logger.LogDebug("Attempting to add FX rate for {BaseCurrency}/{QuoteCurrency}.", fxRate.BaseCurrency, fxRate.QuoteCurrency);
            if (await CurrencyPairExistsAsync(fxRate.BaseCurrency, fxRate.QuoteCurrency))
            {
                _logger.LogWarning("FX rate for {BaseCurrency}/{QuoteCurrency} already exists.", fxRate.BaseCurrency, fxRate.QuoteCurrency);
                return false;
            }

            _context.FxRates.Add(fxRate);
            _logger.LogDebug("FX rate for {BaseCurrency}/{QuoteCurrency} added to context.", fxRate.BaseCurrency, fxRate.QuoteCurrency);
            return true;
        }

        public async Task UpdateAsync(FxRate fxRate)
        {
            _logger.LogDebug("Marking FX rate with id {Id} for update.", fxRate.Id);
            _context.Entry(fxRate).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("FX rate with id {Id} updated successfully.", fxRate.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating FX rate with id {Id}.", fxRate.Id);
                throw;
            }
        }

        public async Task DeleteAsync(FxRate fxRate)
        {
            _logger.LogDebug("Marking FX rate with id {Id} for deletion.", fxRate.Id);
            _context.FxRates.Remove(fxRate);
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("FX rate with id {Id} deleted successfully.", fxRate.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting FX rate with id {Id}.", fxRate.Id);
                throw;
            }
        }

        public async Task<bool> CurrencyPairExistsAsync(string baseCurrency, string quoteCurrency)
        {
            bool exists = await _context.FxRates.AnyAsync(r =>
                r.BaseCurrency == baseCurrency && r.QuoteCurrency == quoteCurrency);
            _logger.LogDebug("Currency pair {BaseCurrency}/{QuoteCurrency} exists: {Exists}.", baseCurrency, quoteCurrency, exists);
            return exists;
        }

        public async Task SaveChangesAsync()
        {
            try
            {
                _logger.LogDebug("Saving changes to the database.");
                await _context.SaveChangesAsync();
                _logger.LogInformation("Database changes saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving changes to the database.");
                throw;
            }
        }
    }
}
