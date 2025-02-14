using CurrencyConverterApi.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverterApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CurrencyController : ControllerBase
    {
        private readonly IExchangeRateService _exchangeRateService;
        private readonly ILogger<CurrencyController> _logger;
        private static readonly List<string> ExcludedCurrencies = new List<string> { "TRY", "PLN", "THB", "MXN" };

        public CurrencyController(IExchangeRateService exchangeRateService, ILogger<CurrencyController> logger)
        {
            _exchangeRateService = exchangeRateService;
            _logger = logger;
        }

        // Endpoint to retrieve the latest exchange rates
        [HttpGet("latest")]
        public async Task<IActionResult> GetLatestRates([FromQuery] string baseCurrency)
        {
            if (baseCurrency == null)
                return BadRequest("Base currency is required.");

            if (ExcludedCurrencies.Contains(baseCurrency.ToUpper()))
            {
                return BadRequest("Conversion involving TRY, PLN, THB, or MXN is not allowed.");
            }
            try
            {
                var rates = await _exchangeRateService.GetLatestRatesAsync(baseCurrency);
                return Ok(rates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving latest exchange rates.");
                return StatusCode(500, "Internal server error");
            }
        }
        
        [HttpGet("history")]
        public async Task<IActionResult> GetHistoricalRates([FromQuery] string baseCurrency, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var historicalRates = await _exchangeRateService.GetHistoricalRates(baseCurrency, startDate, endDate, page, pageSize);
                return Ok(historicalRates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving historical exchange rates.");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("convert")]
        public async Task<IActionResult> ConvertCurrency([FromQuery] string fromCurrency, [FromQuery] string toCurrency, [FromQuery] decimal amount)
        {
            if (ExcludedCurrencies.Contains(fromCurrency.ToUpper()) || ExcludedCurrencies.Contains(toCurrency.ToUpper()))
            {
                return BadRequest("Conversion involving TRY, PLN, THB, or MXN is not allowed.");
            }
        
            try
            {
                var convertedAmount = await _exchangeRateService.ConvertCurrencyAsync(fromCurrency, toCurrency, amount);
                return Ok(convertedAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during currency conversion.");
                return StatusCode(500, "Internal server error");
            }
        }

    }

}
