using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Privestio.Domain.Interfaces;
using Privestio.Infrastructure.PriceFeeds;

namespace Privestio.Infrastructure.Tests.PriceFeeds;

public class YahooFinancePriceFeedProviderTests
{
    [Fact]
    public async Task GetLatestPricesAsync_ToSuffixFallback_UsesBaseSymbolWhenNeeded()
    {
        var requestedPaths = new List<string>();
        using var handler = new StubHttpMessageHandler(request =>
        {
            requestedPaths.Add(request.RequestUri?.PathAndQuery ?? string.Empty);

            var path = request.RequestUri?.PathAndQuery ?? string.Empty;
            if (path.Contains("EEMV.TO", StringComparison.Ordinal))
            {
                return JsonResponse(BuildLatestResponse(0d, "USD"));
            }

            if (path.Contains("EEMV", StringComparison.Ordinal))
            {
                return JsonResponse(BuildLatestResponse(42.25d, "USD"));
            }

            return JsonResponse(BuildLatestResponse(0d, "USD"));
        });

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://query1.finance.yahoo.com/"),
        };
        var provider = new YahooFinancePriceFeedProvider(
            client,
            NullLogger<YahooFinancePriceFeedProvider>.Instance
        );

        var lookup = new PriceLookup(Guid.NewGuid(), "EEMV.TO");
        var result = await provider.GetLatestPricesAsync([lookup]);

        result.Should().HaveCount(1);
        result[0].SecurityId.Should().Be(lookup.SecurityId);
        result[0].Symbol.Should().Be("EEMV.TO");
        result[0].Price.Should().Be(42.25m);
        requestedPaths.Should().Contain(p => p.Contains("EEMV.TO", StringComparison.Ordinal));
        requestedPaths.Should().Contain(p => p.Contains("EEMV?", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetLatestPricesAsync_ToSuffixFallback_ReturnsEmptyWhenNoCandidateHasPrice()
    {
        using var handler = new StubHttpMessageHandler(_ =>
            JsonResponse(BuildLatestResponse(0d, "USD"))
        );

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://query1.finance.yahoo.com/"),
        };
        var provider = new YahooFinancePriceFeedProvider(
            client,
            NullLogger<YahooFinancePriceFeedProvider>.Instance
        );

        var result = await provider.GetLatestPricesAsync([
            new PriceLookup(Guid.NewGuid(), "GSWO.TO"),
        ]);

        result.Should().BeEmpty();
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private static string BuildLatestResponse(double marketPrice, string currency) =>
        $$"""
            {
              "chart": {
                "result": [
                  {
                    "meta": {
                      "currency": "{{currency}}",
                      "regularMarketPrice": {{marketPrice}}
                    }
                  }
                ]
              }
            }
            """;

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        ) => Task.FromResult(_responder(request));
    }
}
