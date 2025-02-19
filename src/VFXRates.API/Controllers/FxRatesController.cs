using Microsoft.AspNetCore.Mvc;
using VFXRates.Application.DTOs;
using VFXRates.Application.Interfaces;

namespace VFXRates.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FxRatesController : ControllerBase
    {
        private readonly IFxRateService _fxRateService;
        private readonly ILogger<FxRatesController> _logger;

        public FxRatesController(IFxRateService fxRateService, ILogger<FxRatesController> logger)
        {
            _fxRateService = fxRateService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FxRateDto>>> GetFxRates()
        {
            var rates = await _fxRateService.GetAllFxRates();
            return Ok(rates);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<FxRateDto>> GetFxRateById(int id)
        {
            var fxRate = await _fxRateService.GetFxRateById(id);
            if (fxRate == null)
            {
                _logger.LogWarning("FX rate with id {Id} not found.", id);
                return NotFound();
            }
            return Ok(fxRate);
        }

        [HttpGet("{baseCurrency:length(3)}/{quoteCurrency:length(3)}")]
        public async Task<ActionResult<FxRateDto>> GetFxRateByCurrencyPair(string baseCurrency, string quoteCurrency)
        {
            var fxRate = await _fxRateService.GetFxRateByCurrencyPair(baseCurrency, quoteCurrency);
            if (fxRate == null)
            {
                _logger.LogWarning("No FX rate found for {BaseCurrency}/{QuoteCurrency}.", baseCurrency, quoteCurrency);
                return NotFound($"No exchange rate found for {baseCurrency}/{quoteCurrency}");
            }
            return Ok(fxRate);
        }

        [HttpPost]
        public async Task<ActionResult<FxRateDto>> PostFxRate(CreateFxRateDto createFxRateDto)
        {
            var createdRate = await _fxRateService.CreateFxRate(createFxRateDto);
            if (createdRate == null)
            {
                _logger.LogWarning("Conflict: FX rate with BaseCurrency {BaseCurrency} and QuoteCurrency {QuoteCurrency} already exists.", createFxRateDto.BaseCurrency, createFxRateDto.QuoteCurrency);
                return Conflict("A currency pair with the specified BaseCurrency and QuoteCurrency already exists.");
            }

            return CreatedAtAction(nameof(GetFxRateByCurrencyPair),
                new { createdRate.BaseCurrency, createdRate.QuoteCurrency },
                createdRate);
        }

        [HttpPut]
        public async Task<IActionResult> PutFxRate(UpdateFxRateDto updateFxRateDto)
        {
            var updatedRate = await _fxRateService.UpdateFxRate(updateFxRateDto);
            if (updatedRate == null)
            {
                _logger.LogWarning("FX rate not found for update: {BaseCurrency}/{QuoteCurrency}.", updateFxRateDto.BaseCurrency, updateFxRateDto.QuoteCurrency);
                return NotFound("FX Rate not found.");
            }
            return Ok(updatedRate);
        }

        [HttpDelete("{baseCurrency:length(3)}/{quoteCurrency:length(3)}")]
        public async Task<IActionResult> DeleteFxRate(string baseCurrency, string quoteCurrency)
        {
            var deleted = await _fxRateService.DeleteFxRate(baseCurrency, quoteCurrency);
            if (!deleted)
            {
                _logger.LogWarning("Failed to delete FX rate for {BaseCurrency}/{QuoteCurrency}.", baseCurrency, quoteCurrency);
                return NotFound();
            }
            return NoContent();
        }
    }
}
