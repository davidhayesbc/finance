using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Privestio.Application.Configuration;
using Privestio.Application.Interfaces;
using Privestio.Infrastructure.Ai;

namespace Privestio.Infrastructure.Tests.Ai;

public class OllamaRuleSuggestionServiceTests
{
    [Fact]
    public async Task SuggestRulesAsync_ModelMissing_PullsAndRetriesSuccessfully()
    {
        var calls = new List<string>();
        using var handler = new StubHttpMessageHandler(request =>
        {
            var path = request.RequestUri?.PathAndQuery ?? string.Empty;
            calls.Add(path);

            if (path == "/api/chat")
            {
                var chatCalls = calls.Count(c => c == "/api/chat");
                if (chatCalls == 1)
                {
                    return JsonResponse(
                        HttpStatusCode.NotFound,
                        "{\"error\":\"model 'llama3.1:8b' not found\"}"
                    );
                }

                return JsonResponse(
                    HttpStatusCode.OK,
                    "{\"message\":{\"content\":\"{\\\"suggestions\\\":[{\\\"name\\\":\\\"Groceries\\\",\\\"descriptionContains\\\":\\\"SAVE ON FOODS\\\",\\\"minAmount\\\":null,\\\"maxAmount\\\":null,\\\"suggestedCategoryName\\\":\\\"Groceries\\\",\\\"rationale\\\":\\\"Recurring merchant\\\"}]}\"}}"
                );
            }

            if (path == "/api/pull")
            {
                return JsonResponse(HttpStatusCode.OK, "{\"status\":\"success\"}");
            }

            return JsonResponse(HttpStatusCode.NotFound, "{}");
        });

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434/"),
        };

        var service = CreateService(client);
        var rows = new List<RuleSuggestionInputRow> { new("SAVE ON FOODS", 132.44m) };

        var suggestions = await service.SuggestRulesAsync(rows, 3);

        suggestions.Should().HaveCount(1);
        suggestions[0].Name.Should().Be("Groceries");
        calls.Count(c => c == "/api/chat").Should().Be(2);
        calls.Should().Contain("/api/pull");
    }

    [Fact]
    public async Task SuggestRulesAsync_ModelMissingAndPullFails_ThrowsInvalidOperationException()
    {
        using var handler = new StubHttpMessageHandler(request =>
        {
            var path = request.RequestUri?.PathAndQuery ?? string.Empty;
            if (path == "/api/chat")
            {
                return JsonResponse(
                    HttpStatusCode.NotFound,
                    "{\"error\":\"model 'llama3.1:8b' not found\"}"
                );
            }

            if (path == "/api/pull")
            {
                return JsonResponse(HttpStatusCode.InternalServerError, "{\"error\":\"pull failed\"}");
            }

            return JsonResponse(HttpStatusCode.NotFound, "{}");
        });

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434/"),
        };

        var service = CreateService(client);
        var rows = new List<RuleSuggestionInputRow> { new("SAVE ON FOODS", 132.44m) };

        var act = () => service.SuggestRulesAsync(rows, 3);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*status 404*");
    }

    [Fact]
    public async Task SuggestRulesAsync_WhenOllamaRequestTimesOut_ThrowsTimeoutException()
    {
        using var handler = new StubHttpMessageHandler(_ =>
        {
            throw new TaskCanceledException("The request timed out.");
        });

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:11434/"),
        };

        var service = CreateService(client);
        var rows = new List<RuleSuggestionInputRow> { new("SAVE ON FOODS", 132.44m) };

        var act = () => service.SuggestRulesAsync(rows, 3);

        await act.Should()
            .ThrowAsync<TimeoutException>()
            .WithMessage("*timed out*");
    }

    private static OllamaRuleSuggestionService CreateService(HttpClient client)
    {
        var options = Options.Create(
            new OllamaOptions
            {
                Model = "llama3.1:8b",
                Temperature = 0.1,
            }
        );

        return new OllamaRuleSuggestionService(
            client,
            options,
            NullLogger<OllamaRuleSuggestionService>.Instance
        );
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

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
