using CurrencyConverterApi.Models;

namespace CurrencyConverterApi.Services
{
    public interface IExchangeRateService
    {
        Task<Dictionary<string, decimal>> GetLatestRatesAsync(string baseCurrency);
        Task<decimal> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount);
        //Task<IEnumerable<ExchangeRateHistory>> GetHistoricalRatesAsync(string baseCurrency, DateTime startDate, DateTime endDate, int page, int pageSize);
        Task<CurrencyRatesResponse> GetHistoricalRates(string baseCurrency, DateTime startDate, DateTime endDate, int page, int pageSize);
    }

}
