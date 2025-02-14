using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Xunit;
using CurrencyConverterApi.Services;
using Moq.Protected;
using CurrencyConverterApi.Models;

public class ExchangeRateServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly ExchangeRateService _service;
    private readonly Mock<ILogger<ExchangeRateService>> _loggerMock;

    public ExchangeRateServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockCache = new Mock<IMemoryCache>();
        _loggerMock = new Mock<ILogger<ExchangeRateService>>();

        // Create a mock of HttpMessageHandler to mock HttpClient behavior
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        // Setup the mock to return a fake response for GetStringAsync
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<System.Threading.CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(new ExchangeRateResponse
                {
                    Rates = new Dictionary<string, decimal>
                    {
                        { "EUR", 0.85m },
                        { "GBP", 0.75m }
                    }
                }))
            });

        // Create HttpClient using the mocked handler
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);

        // Setup IHttpClientFactory to return the mocked HttpClient
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        // Initialize the service
        _service = new ExchangeRateService(_mockHttpClientFactory.Object, _mockCache.Object, _loggerMock.Object);
    }
    #region GetLatestRates
    [Fact]
    public async Task GetLatestRatesAsync_ShouldReturnRates_WhenApiCallIsSuccessful()
    {
        // Arrange
        string baseCurrency = "USD";

        // Mock memory cache to simulate cache miss
        _mockCache.Setup(cache => cache.TryGetValue(It.IsAny<object>(), out It.Ref<Dictionary<string, decimal>>.IsAny))
                  .Returns(false);

        // Act
        var result = await _service.GetLatestRatesAsync(baseCurrency);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("EUR"));
        Assert.True(result.ContainsKey("GBP"));
        Assert.Equal(0.85m, result["EUR"]);
        Assert.Equal(0.75m, result["GBP"]);

        // Verify that the result is cached
        _mockCache.Verify(cache => cache.Set(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<TimeSpan>()), Times.Once);
    }
    
    [Fact]
    public async Task GetLatestRatesAsync_ShouldHandleApiError_WhenApiCallFails()
    {
        // Arrange
        string baseCurrency = "USD";

        // Create a mock of HttpMessageHandler to simulate API failure
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                It.IsAny<HttpRequestMessage>(),
                It.IsAny<System.Threading.CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("API request failed"));

        // Create HttpClient using the mocked handler
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);

        // Setup IHttpClientFactory to return the mocked HttpClient
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        
        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(async () => await _service.GetLatestRatesAsync(baseCurrency));

        // Optionally verify that the correct logging was done or retry mechanism was triggered
    }
    #endregion

    #region CurrencyConversion
    

    [Fact]
    public async Task ConvertCurrencyAsync_ShouldConvert_WhenRateExists()
    {
        // Arrange
        string fromCurrency = "USD";
        string toCurrency = "EUR";
        decimal amount = 100m;

        // Act
        var result = await _service.ConvertCurrencyAsync(fromCurrency, toCurrency, amount);

        // Assert
        Assert.Equal(85m, result); // 100 * 0.85 (EUR rate)
    }

    [Fact]
    public async Task ConvertCurrencyAsync_ShouldThrowException_WhenRateNotFound()
    {
        // Arrange
        string fromCurrency = "USD";
        string toCurrency = "AUDI";
        decimal amount = 100m;

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => await _service.ConvertCurrencyAsync(fromCurrency, toCurrency, amount));
    }
    #endregion

    #region Historical Data
    [Fact]
    public async Task GetHistoricalRates_ShouldReturnPaginatedRates()
    {
        // Arrange
        string baseCurrency = "USD";
        DateTime startDate = new DateTime(2021, 01, 01);
        DateTime endDate = new DateTime(2021, 01, 03);
        int page = 1;
        int pageSize = 2;

        // Act
        var result = await _service.GetHistoricalRates(baseCurrency, startDate, endDate, page, pageSize);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Rates.Count);  // Only 2 items should be returned due to pagination (pageSize = 2)
        Assert.Equal(0.85m, Convert.ToDecimal(result.Rates[0].Rates.Values)); // First rate for 2021-01-01
        Assert.Equal(0.88m, Convert.ToDecimal(result.Rates[1].Rates.Values)); // Second rate for 2021-01-02
    }

    [Fact]
    public async Task GetHistoricalRates_ShouldReturnEmpty_WhenNoDataAvailableForPage()
    {
        // Arrange
        string baseCurrency = "USD";
        DateTime startDate = new DateTime(2021, 01, 01);
        DateTime endDate = new DateTime(2021, 01, 03);
        int page = 2; // This page is empty, since only 3 records exist
        int pageSize = 2;

        // Act
        var result = await _service.GetHistoricalRates(baseCurrency, startDate, endDate, page, pageSize);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Rates); // No rates should be returned for this page
    }
    #endregion
}
