using System.Security.Principal;

namespace CurrencyConverterApi.Models
{
    public class ExchangeRateResponse
    {
        
        public string Base { get; set; } = string.Empty;
        public Dictionary<string, decimal> Rates { get; set; }
        public DateTime Date { get; set; }
    }
}
