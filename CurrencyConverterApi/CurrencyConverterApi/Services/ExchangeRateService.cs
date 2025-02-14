using CurrencyConverterApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace CurrencyConverterApi.Services
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ExchangeRateService> _logger;
        private readonly string _latestRatesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "latest_exchange_rates.json");
        private readonly string _historicalRatesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "historical_exchange_rates.json");
        public ExchangeRateService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<ExchangeRateService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }
        public async Task<ExchangeRateResponse> GetLatestExchangeRatesFromFileAsync()
        {
            var json = await File.ReadAllTextAsync(_latestRatesFilePath);
            var data = JsonConvert.DeserializeObject<ExchangeRateResponse>(json);
            return data;
        }
        private CurrencyRatesResponse GetHistoryFromFile()
        {
            var json = File.ReadAllText(_historicalRatesFilePath);
            var data = JsonConvert.DeserializeObject<CurrencyRatesResponse>(json);
            return data;
        }
        public async Task<Dictionary<string, decimal>> GetLatestRatesAsync(string baseCurrency)
        {
            var cacheKey = $"LatestRates_{baseCurrency}";
            if (_cache.TryGetValue(cacheKey, out Dictionary<string, decimal> cachedRates))
            {
                return cachedRates;
            }

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetStringAsync($"https://api.exchangerate-api.com/v4/latest/{baseCurrency}");
           var rates = JsonConvert.DeserializeObject<ExchangeRateResponse>(response).Rates;

            _cache.Set(cacheKey, rates, TimeSpan.FromMinutes(5)); // Cache for 5 minutes
            return rates;
        }

        public async Task<decimal> ConvertCurrencyAsync(string fromCurrency, string toCurrency, decimal amount)
        {
            var rates = await GetLatestRatesAsync(fromCurrency);
            if (rates.ContainsKey(toCurrency.ToUpper()))
            {
                return amount * rates.Where(x=>x.Key == toCurrency.ToUpper()).Select(x=>x.Value).First();
            }

            throw new Exception($"Conversion rate not found for {toCurrency}");
        }

     
        public async Task<CurrencyRatesResponse> GetHistoricalRates(string baseCurrency, DateTime startDate, DateTime endDate, int page, int pageSize)
        {
            
            var client = _httpClientFactory.CreateClient();
            var responsefile = GetHistoryFromFile();
            var skip = (page - 1) * pageSize;
            responsefile.Rates = responsefile.Rates.Skip(skip).Take(pageSize).ToList();
            return responsefile;
           
        }
   

    }

}
