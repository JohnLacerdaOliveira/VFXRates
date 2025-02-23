using VFXRates.Application.DTOs;
using VFXRates.Application.Interfaces;
using VFXRates.Domain.Entities;

namespace VFXRates.Application.Services
{
    public class FxRateService : IFxRateService
    {
        private readonly IFxRatesRepository _fxRateRepository;
        private readonly IExchangeRateApiClient _exchangeRateApiClient;
        private readonly ILogService _logService;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;

        public FxRateService(IFxRatesRepository fxRateRepository,
                             IExchangeRateApiClient exchangeRateApiClient,
                             IRabbitMqPublisher rabbitMqPublisher,
                             ILogService logService)
        {
            _fxRateRepository = fxRateRepository;
            _exchangeRateApiClient = exchangeRateApiClient;
            _rabbitMqPublisher = rabbitMqPublisher;
            _logService = logService;
        }

        public async Task<IEnumerable<FxRateDto>> GetAllFxRates()
        {
            await _logService.LogDebug("Retrieving all FX rates.");
            var rates = await _fxRateRepository.GetAllAsync();

            return rates.Select(r => MapToDto(r));
        }

        public async Task<FxRateDto?> GetFxRateById(int id)
        {
            await _logService.LogDebug($"Retrieving FX rate with id {id}.");
            var fxRate = await _fxRateRepository.GetByIdAsync(id);
            if (fxRate == null)
            {
                await _logService.LogWarning($"FX rate with id {id} not found.");
                return null;
            }
            return MapToDto(fxRate);
        }

        public async Task<FxRateDto?> GetFxRateByCurrencyPair(string baseCurrency, string quoteCurrency)
        {
            (baseCurrency, quoteCurrency) = NormalizeCurrencyPair(baseCurrency, quoteCurrency);

            await _logService.LogDebug($"Retrieving FX rate for {baseCurrency}/{quoteCurrency} from repository.");
            var existingRate = await _fxRateRepository.GetByCurrencyPairAsync(baseCurrency, quoteCurrency);
            if (existingRate != null)
            {
                await _logService.LogDebug($"Found FX rate for {baseCurrency}/{quoteCurrency} in repository.");
                return MapToDto(existingRate);
            }

            await _logService.LogDebug($"FX rate for {baseCurrency}/{quoteCurrency} not found in repository. Fetching from external API.");
            var fetchedRate = await _exchangeRateApiClient.FetchRateAsync(baseCurrency, quoteCurrency);
            if (fetchedRate == null)
            {
                await _logService.LogWarning($"External API did not return a rate for {baseCurrency}/{quoteCurrency}.");
                return null;
            }

            var fxRate = new FxRate
            {
                BaseCurrency = fetchedRate.BaseCurrency,
                QuoteCurrency = fetchedRate.QuoteCurrency,
                Bid = fetchedRate.Bid,
                Ask = fetchedRate.Ask,
                LastUpdated = DateTime.UtcNow
            };

            await _logService.LogDebug($"Saving fetched FX rate for {baseCurrency}/{quoteCurrency} to repository.");
            await _fxRateRepository.AddAsync(fxRate);
            await _fxRateRepository.SaveChangesAsync();

            await PublishNewFxRateEventAsync(fxRate);

            return MapToDto(fxRate);
        }

        public async Task<FxRateDto?> CreateFxRate(CreateFxRateDto createFxRateDto)
        {
            NormalizeCurrencyDto(createFxRateDto);

            await _logService.LogDebug($"Checking if FX rate for {createFxRateDto.BaseCurrency}/{createFxRateDto.QuoteCurrency} already exists.");

            var existingFxRate = await _fxRateRepository.GetByCurrencyPairAsync(createFxRateDto.BaseCurrency, createFxRateDto.QuoteCurrency);

            if (existingFxRate != null)
            {
                await _logService.LogWarning($"FX rate for {createFxRateDto.BaseCurrency}/{createFxRateDto.QuoteCurrency} already exists.");
                return null;
            }

            await _logService.LogDebug($"Creating new FX rate for {createFxRateDto.BaseCurrency}/{createFxRateDto.QuoteCurrency}.");

            var fxRate = new FxRate
            {
                BaseCurrency = createFxRateDto.BaseCurrency,
                QuoteCurrency = createFxRateDto.QuoteCurrency,
                Bid = createFxRateDto.Bid,
                Ask = createFxRateDto.Ask,
                LastUpdated = DateTime.UtcNow
            };

            await _logService.LogInformation($"Created new FX rate for {fxRate.BaseCurrency}/{fxRate.QuoteCurrency}.");
            await _fxRateRepository.AddAsync(fxRate);
            await _fxRateRepository.SaveChangesAsync();

            await PublishNewFxRateEventAsync(fxRate);

            return MapToDto(fxRate);
        }


        public async Task<FxRateDto?> UpdateFxRate(UpdateFxRateDto updateFxRateDto)
        {
            NormalizeCurrencyDto(updateFxRateDto);

            await _logService.LogDebug($"Updating FX rate for {updateFxRateDto.BaseCurrency}/{updateFxRateDto.QuoteCurrency}.");
            var fxRate = await _fxRateRepository.GetByCurrencyPairAsync(updateFxRateDto.BaseCurrency, updateFxRateDto.QuoteCurrency);
            if (fxRate == null)
            {
                await _logService.LogWarning($"FX rate not found for update: {updateFxRateDto.BaseCurrency}/{updateFxRateDto.QuoteCurrency}.");
                return null;
            }

            // Update the properties of the existing entity
            fxRate.Bid = updateFxRateDto.Bid;
            fxRate.Ask = updateFxRateDto.Ask;
            fxRate.LastUpdated = DateTime.UtcNow;

            await _fxRateRepository.UpdateAsync(fxRate);
            await _fxRateRepository.SaveChangesAsync();

            await _logService.LogInformation($"Updated FX rate for {fxRate.BaseCurrency}/{fxRate.QuoteCurrency}.");
            return MapToDto(fxRate);
        }

        public async Task<bool> DeleteFxRate(string baseCurrency, string quoteCurrency)
        {
            (baseCurrency, quoteCurrency) = NormalizeCurrencyPair(baseCurrency, quoteCurrency);

            await _logService.LogDebug($"Deleting FX rate for {baseCurrency}/{quoteCurrency}.");
            var fxRate = await _fxRateRepository.GetByCurrencyPairAsync(baseCurrency, quoteCurrency);
            if (fxRate == null)
            {
                await _logService.LogWarning($"FX rate not found for deletion: {baseCurrency}/{quoteCurrency}.");
                return false;
            }

            await _fxRateRepository.DeleteAsync(fxRate);
            await _fxRateRepository.SaveChangesAsync();

            await _logService.LogInformation($"Deleted FX rate for {baseCurrency}/{quoteCurrency}.");
            return true;
        }

        private static FxRateDto MapToDto(FxRate fxRate) => new FxRateDto
        {
            BaseCurrency = fxRate.BaseCurrency,
            QuoteCurrency = fxRate.QuoteCurrency,
            Bid = fxRate.Bid,
            Ask = fxRate.Ask,
            LastUpdated = fxRate.LastUpdated
        };

        public static void NormalizeCurrencyDto(ICurrencyDto dto)
        {
            ArgumentNullException.ThrowIfNull(dto);

            dto.BaseCurrency = dto.BaseCurrency.ToUpper().Trim();
            dto.QuoteCurrency = dto.QuoteCurrency.ToUpper().Trim();
        }


        public static (string baseCurrency, string quoteCurrency) NormalizeCurrencyPair(string baseCurrency, string quoteCurrency)
        {
            return (baseCurrency.ToUpper().Trim(), quoteCurrency.ToUpper().Trim());
        }

        private async Task PublishNewFxRateEventAsync(FxRate fxRate)
        {
            try
            {
                await _rabbitMqPublisher.PublishFxRateCreation(fxRate);
                await _logService.LogInformation($"Published new FX rate event for {fxRate.BaseCurrency}/{fxRate.QuoteCurrency} via RabbitMQ.");
            }
            catch (Exception ex)
            {
                await _logService.LogError($"Failed to publish new FX rate event for {fxRate.BaseCurrency}/{fxRate.BaseCurrency}.", ex);
            }
        }
    }
}
