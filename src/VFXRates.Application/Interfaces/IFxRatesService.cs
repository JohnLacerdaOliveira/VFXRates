using VFXRates.Application.DTOs;

namespace VFXRates.Application.Interfaces
{
    public interface IFxRateService
    {
        Task<IEnumerable<FxRateDto>> GetAllFxRates();
        Task<FxRateDto?> GetFxRateById(int id);
        Task<FxRateDto?> GetFxRateByCurrencyPair(string baseCurrency, string quoteCurrency);
        Task<FxRateDto?> CreateFxRate(CreateFxRateDto fxRateDto);
        Task<FxRateDto?> UpdateFxRate(UpdateFxRateDto fxRateDto);
        Task<bool> DeleteFxRate(string baseCurrency, string quoteCurrency);
    }
}
