using Microsoft.Extensions.Logging;
using VFXRates.Application.DTOs;
using VFXRates.Application.Interfaces;
using VFXRates.Domain.Entities;

namespace VFXRates.Application.Services
{
    public class FxRateService : IFxRateService
    {
        private readonly IFxRateRepository _fxRateRepository;
        private readonly IExchangeRateApiClient _exchangeRateApiClient;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly ILogger<FxRateService> _logger;

        public FxRateService(IFxRateRepository fxRateRepository,
                             IExchangeRateApiClient exchangeRateApiClient,
                             IRabbitMqPublisher rabbitMqPublisher,
                             ILogger<FxRateService> logger)
        {
            _fxRateRepository = fxRateRepository;
            _exchangeRateApiClient = exchangeRateApiClient;
            _rabbitMqPublisher = rabbitMqPublisher;
            _logger = logger;
        }

        public async Task<IEnumerable<FxRateDto>> GetAllFxRates()
        {
            _logger.LogDebug("Retrieving all FX rates.");
            var rates = await _fxRateRepository.GetAllAsync();
            _logger.LogDebug("Retrieved {Count} FX rates.", rates.Count());
            return rates.Select(r => MapToDto(r));
        }

        public async Task<FxRateDto?> GetFxRateById(int id)
        {
            _logger.LogDebug("Retrieving FX rate with id {Id}.", id);
            var fxRate = await _fxRateRepository.GetByIdAsync(id);
            if (fxRate == null)
            {
                _logger.LogWarning("FX rate with id {Id} not found.", id);
                return null;
            }
            return MapToDto(fxRate);
        }

        public async Task<FxRateDto?> GetFxRateByCurrencyPair(string baseCurrency, string quoteCurrency)
        {
            (baseCurrency, quoteCurrency) = NormalizeCurrencyPair(baseCurrency, quoteCurrency);

            _logger.LogDebug("Retrieving FX rate for {BaseCurrency}/{QuoteCurrency} from repository.", baseCurrency, quoteCurrency);
            var existingRate = await _fxRateRepository.GetByCurrencyPairAsync(baseCurrency, quoteCurrency);
            if (existingRate != null)
            {
                _logger.LogDebug("Found FX rate for {BaseCurrency}/{QuoteCurrency} in repository.", baseCurrency, quoteCurrency);
                return MapToDto(existingRate);
            }

            _logger.LogDebug("FX rate for {BaseCurrency}/{QuoteCurrency} not found in repository. Fetching from external API.", baseCurrency, quoteCurrency);
            var fetchedRate = await _exchangeRateApiClient.FetchRateAsync(baseCurrency, quoteCurrency);
            if (fetchedRate == null)
            {
                _logger.LogWarning("External API did not return a rate for {BaseCurrency}/{QuoteCurrency}.", baseCurrency, quoteCurrency);
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

            _logger.LogDebug("Saving fetched FX rate for {BaseCurrency}/{QuoteCurrency} to repository.", baseCurrency, quoteCurrency);
            await _fxRateRepository.AddAsync(fxRate);
            await _fxRateRepository.SaveChangesAsync();

            await PublishNewFxRateEventAsync(fxRate);
           
            return MapToDto(fxRate);
        }

        public async Task<FxRateDto?> CreateFxRate(CreateFxRateDto createFxRateDto)
        {
            NormalizeCurrencyDto(createFxRateDto);

            _logger.LogDebug("Checking if FX rate for {BaseCurrency}/{QuoteCurrency} already exists.",
         createFxRateDto.BaseCurrency, createFxRateDto.QuoteCurrency);

            // Check if the rate already exists
            var existingFxRate = await _fxRateRepository.GetByCurrencyPairAsync(createFxRateDto.BaseCurrency, createFxRateDto.QuoteCurrency);

            if (existingFxRate != null)
            {
                _logger.LogWarning("FX rate for {BaseCurrency}/{QuoteCurrency} already exists.",
                    createFxRateDto.BaseCurrency, createFxRateDto.QuoteCurrency);
                return null; // or throw an exception if you prefer
            }

            _logger.LogDebug("Creating new FX rate for {BaseCurrency}/{QuoteCurrency}.",
                createFxRateDto.BaseCurrency, createFxRateDto.QuoteCurrency);

            var fxRate = new FxRate
            {
                BaseCurrency = createFxRateDto.BaseCurrency,
                QuoteCurrency = createFxRateDto.QuoteCurrency,
                Bid = createFxRateDto.Bid,
                Ask = createFxRateDto.Ask,
                LastUpdated = DateTime.UtcNow
            };

            _logger.LogInformation("Created new FX rate for {BaseCurrency}/{QuoteCurrency}.",
               fxRate.BaseCurrency, fxRate.QuoteCurrency);
            await _fxRateRepository.AddAsync(fxRate);
            await _fxRateRepository.SaveChangesAsync();

            await PublishNewFxRateEventAsync(fxRate);

            return MapToDto(fxRate);
        }


        public async Task<FxRateDto?> UpdateFxRate(UpdateFxRateDto updateFxRateDto)
        {
            NormalizeCurrencyDto(updateFxRateDto);

            _logger.LogDebug("Updating FX rate for {BaseCurrency}/{QuoteCurrency}.", updateFxRateDto.BaseCurrency, updateFxRateDto.QuoteCurrency);
            var fxRate = await _fxRateRepository.GetByCurrencyPairAsync(updateFxRateDto.BaseCurrency, updateFxRateDto.QuoteCurrency);
            if (fxRate == null)
            {
                _logger.LogWarning("FX rate not found for update: {BaseCurrency}/{QuoteCurrency}.", updateFxRateDto.BaseCurrency, updateFxRateDto.QuoteCurrency);
                return null;
            }

            fxRate.Bid = updateFxRateDto.Bid;
            fxRate.Ask = updateFxRateDto.Ask;
            fxRate.LastUpdated = DateTime.UtcNow;

            await _fxRateRepository.UpdateAsync(fxRate);
            await _fxRateRepository.SaveChangesAsync();

            _logger.LogInformation("Updated FX rate for {BaseCurrency}/{QuoteCurrency}.", fxRate.BaseCurrency, fxRate.QuoteCurrency);
            return MapToDto(fxRate);
        }

        public async Task<bool> DeleteFxRate(string baseCurrency, string quoteCurrency)
        {
            (baseCurrency, quoteCurrency) = NormalizeCurrencyPair(baseCurrency, quoteCurrency);

            _logger.LogDebug("Deleting FX rate for {BaseCurrency}/{QuoteCurrency}.", baseCurrency, quoteCurrency);
            var fxRate = await _fxRateRepository.GetByCurrencyPairAsync(baseCurrency, quoteCurrency);
            if (fxRate == null)
            {
                _logger.LogWarning("FX rate not found for deletion: {BaseCurrency}/{QuoteCurrency}.", baseCurrency, quoteCurrency);
                return false;
            }

            await _fxRateRepository.DeleteAsync(fxRate);
            await _fxRateRepository.SaveChangesAsync();

            _logger.LogInformation("Deleted FX rate for {BaseCurrency}/{QuoteCurrency}.", baseCurrency, quoteCurrency);
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
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

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
                _logger.LogInformation("Published new FX rate event for {BaseCurrency}/{QuoteCurrency} via RabbitMQ.", fxRate.BaseCurrency, fxRate.QuoteCurrency);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish new FX rate event for {BaseCurrency}/{QuoteCurrency}.", fxRate.BaseCurrency, fxRate.QuoteCurrency);
            }
        }
    }
}
