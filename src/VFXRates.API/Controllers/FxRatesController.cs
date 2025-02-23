using Microsoft.AspNetCore.Authorization;
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
        private readonly ILogService _logService;

        public FxRatesController(IFxRateService fxRateService, ILogService logService)
        {
            _fxRateService = fxRateService;
            _logService = logService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FxRateDto>>> GetFxRates()
        {
            var rates = await _fxRateService.GetAllFxRates();
            return Ok(rates);
        }

        [HttpGet("id/{id:int}")]
        public async Task<ActionResult<FxRateDto>> GetFxRateById(int id)
        {
            var fxRate = await _fxRateService.GetFxRateById(id);
            if (fxRate == null)
            {
                await _logService.LogWarning($"FX rate with id {id} not found.");
                return NotFound();
            }
            return Ok(fxRate);
        }

        [HttpGet("pair/{baseCurrency}/{quoteCurrency}")]
        public async Task<ActionResult<FxRateDto>> GetFxRateByCurrencyPair(string baseCurrency, string quoteCurrency)
        {
            var fxRate = await _fxRateService.GetFxRateByCurrencyPair(baseCurrency, quoteCurrency);
            if (fxRate == null)
            {
                await _logService.LogWarning($"No FX rate found for {baseCurrency}/{quoteCurrency}.");
                return NotFound();
            }
            return Ok(fxRate);
        }
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<FxRateDto>> PostFxRate([FromBody] CreateFxRateDto createFxRateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var createdRate = await _fxRateService.CreateFxRate(createFxRateDto);
            if (createdRate == null)
            {
                await _logService.LogWarning($"Conflict: FX rate with BaseCurrency {createFxRateDto.BaseCurrency} and QuoteCurrency {createFxRateDto.QuoteCurrency} already exists.");

                return Conflict();
            }

            await _logService.LogInformation($"Created FX rate for {createdRate.BaseCurrency}/{createdRate.QuoteCurrency}.");

            return CreatedAtAction(nameof(GetFxRateByCurrencyPair),
                new { createdRate.BaseCurrency, createdRate.QuoteCurrency },
                createdRate);
        }

        [HttpPut("{baseCurrency:length(3)}/{quoteCurrency:length(3)}")]
        [Authorize]
        public async Task<IActionResult> PutFxRate(string baseCurrency, string quoteCurrency, UpdateFxRateDto updateFxRateDto)
        {
            if (baseCurrency != updateFxRateDto.BaseCurrency || quoteCurrency != updateFxRateDto.QuoteCurrency)
            {
                await _logService.LogWarning($"Mismatch between route ({baseCurrency}/{quoteCurrency}) and DTO ({updateFxRateDto.BaseCurrency}/{updateFxRateDto.QuoteCurrency}).");

                return BadRequest("Currency pair in route must match request body.");
            }
            var updatedRate = await _fxRateService.UpdateFxRate(updateFxRateDto);
            if (updatedRate == null)
            {
                await _logService.LogWarning($"FX rate not found for update: {updateFxRateDto.BaseCurrency}/{updateFxRateDto.QuoteCurrency}.");
                return NotFound();
            }

            await _logService.LogInformation($"Updated FX rate for {updatedRate.BaseCurrency}/{updatedRate.QuoteCurrency}.");
            return Ok(updatedRate);
        }

        [HttpDelete("{baseCurrency:length(3)}/{quoteCurrency:length(3)}")]
        [Authorize]
        public async Task<IActionResult> DeleteFxRate(string baseCurrency, string quoteCurrency)
        {
            var deleted = await _fxRateService.DeleteFxRate(baseCurrency, quoteCurrency);
            if (!deleted)
            {
                await _logService.LogWarning($"Failed to delete FX rate for {baseCurrency}/{quoteCurrency}.");
                return NotFound();
            }

            await _logService.LogInformation($"Deleted FX rate for {baseCurrency}/{quoteCurrency}.");
            return NoContent();
        }
    }
}
