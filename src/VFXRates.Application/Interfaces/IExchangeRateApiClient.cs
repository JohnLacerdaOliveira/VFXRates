using VFXRates.Application.DTOs;

namespace VFXRates.Application.Interfaces
{
    public interface IExchangeRateApiClient
    {
        Task<FxRateDto?> FetchRateAsync(string baseCurrency, string quoteCurrency);

    }
}
