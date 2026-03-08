using System.Net.Http.Json;
using Privestio.Contracts.Responses;

namespace Privestio.Web.Services;

public interface INotificationWebService
{
    Task<IReadOnlyList<NotificationResponse>> GetNotificationsAsync(
        bool includeRead = false,
        int? limit = null
    );
    Task<bool> MarkAsReadAsync(Guid id);
    Task<bool> MarkAllAsReadAsync();
    Task<bool> CheckAlertsAsync(
        decimal? minimumBalance = null,
        int? year = null,
        int? month = null
    );
    Task<CashFlowForecastResponse?> GetCashFlowForecastAsync(int months = 6);
}

public class NotificationWebService : INotificationWebService
{
    private readonly HttpClient _httpClient;

    public NotificationWebService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<IReadOnlyList<NotificationResponse>> GetNotificationsAsync(
        bool includeRead = false,
        int? limit = null
    )
    {
        try
        {
            var url = "/api/v1/notifications";
            var queryParams = new List<string>();
            if (includeRead)
                queryParams.Add("includeRead=true");
            if (limit.HasValue)
                queryParams.Add($"limit={limit.Value}");
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var notifications = await _httpClient.GetFromJsonAsync<List<NotificationResponse>>(url);
            return notifications ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<bool> MarkAsReadAsync(Guid id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/api/v1/notifications/{id}/read", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> MarkAllAsReadAsync()
    {
        try
        {
            var response = await _httpClient.PostAsync("/api/v1/notifications/read-all", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> CheckAlertsAsync(
        decimal? minimumBalance = null,
        int? year = null,
        int? month = null
    )
    {
        try
        {
            var url = "/api/v1/notifications/check-alerts";
            var queryParams = new List<string>();
            if (minimumBalance.HasValue)
                queryParams.Add($"minimumBalance={minimumBalance.Value}");
            if (year.HasValue)
                queryParams.Add($"year={year.Value}");
            if (month.HasValue)
                queryParams.Add($"month={month.Value}");
            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var response = await _httpClient.PostAsync(url, null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<CashFlowForecastResponse?> GetCashFlowForecastAsync(int months = 6)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CashFlowForecastResponse>(
                $"/api/v1/forecast/cash-flow?months={months}"
            );
        }
        catch
        {
            return null;
        }
    }
}
