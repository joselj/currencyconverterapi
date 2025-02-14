namespace CurrencyConverterApi.Models
{
    public class ExchangeRateHistory
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }

public class CurrencyRatesResponse
    {
       public List<CurrencyRateEntry> Rates { get; set; }
    }

    public class CurrencyRateEntry
    {
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }

}
