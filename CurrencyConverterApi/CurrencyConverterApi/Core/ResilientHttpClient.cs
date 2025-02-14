using Polly;

namespace CurrencyConverterApi.Core
{
    public class ResilientHttpClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ResilientHttpClient> _logger;

        public ResilientHttpClient(IHttpClientFactory httpClientFactory, ILogger<ResilientHttpClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> GetWithRetryAsync(string url)
        {
            var policy = Policy.Handle<HttpRequestException>()
                               .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                               .RetryAsync(3, (exception, retryCount) =>
                               {
                                   if (exception != null)
                                   {
                                       _logger.LogWarning($"Request failed with exception: {exception.Exception.Message}. Retrying {retryCount}...");
                                   }
                                   else
                                   {
                                       _logger.LogWarning($"Request failed with status code: {exception?.Exception.InnerException }. Retrying {retryCount}...");
                                   }
                               });

            var client = _httpClientFactory.CreateClient();

            // Execute the policy and return only the content as a string
            var response = await policy.ExecuteAsync(async () =>
            {
                var httpResponse = await client.GetAsync(url);
                return httpResponse;  // Return HttpResponseMessage
            });

            // Ensure we have a successful response
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Request failed with status code {response.StatusCode}");
            }

            // Return the content as a string after the successful retry
            return await response.Content.ReadAsStringAsync();
        }
    }


}
