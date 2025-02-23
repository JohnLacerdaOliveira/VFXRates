using VFXRates.Domain.Entities;

namespace VFXRates.Application.Interfaces
{
    public interface IFxRatesRepository
    {
        Task<IEnumerable<FxRate>> GetAllAsync();
        Task<FxRate?> GetByIdAsync(int id);
        Task<FxRate?> GetByCurrencyPairAsync(string baseCurrency, string quoteCurrency);
        Task<bool> AddAsync(FxRate fxRate);
        Task UpdateAsync(FxRate fxRate);
        Task DeleteAsync(FxRate fxRate);
        Task<bool> CurrencyPairExistsAsync(string baseCurrency, string quoteCurrency);
        Task SaveChangesAsync();
    }
}
