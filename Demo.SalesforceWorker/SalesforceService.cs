using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Demo.Shared.Events;
using System.Collections.Concurrent;
using System.Text;

namespace Demo.SalesforceWorker;





public class SalesforceService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<SalesforceService> _logger;
    private readonly ConcurrentDictionary<Guid, bool> _processed = new();

    private string? _accessToken;
    private DateTime _tokenExpires = DateTime.MinValue;

    public SalesforceService(IHttpClientFactory httpFactory, IConfiguration config, ILogger<SalesforceService> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(_accessToken) && _tokenExpires > DateTime.UtcNow.AddSeconds(30))
            return _accessToken;

        var tokenUrl = _config["Salesforce:TokenUrl"];
        var clientId = _config["Salesforce:ClientId"];
        var clientSecret = _config["Salesforce:ClientSecret"];

        if (string.IsNullOrEmpty(tokenUrl) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            _logger.LogError("Salesforce credentials not configured");
            return null;
        }

        try
        {
            var client = _httpFactory.CreateClient();
            var form = new[] {
                new KeyValuePair<string,string>("grant_type","client_credentials"),
                new KeyValuePair<string,string>("client_id", clientId),
                new KeyValuePair<string,string>("client_secret", clientSecret),
            };
            var resp = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(form));
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get Salesforce token: {Status}", resp.StatusCode);
                return null;
            }
            var text = await resp.Content.ReadAsStringAsync();
            dynamic doc = JsonConvert.DeserializeObject(text);
            _accessToken = (string?)doc?.access_token;
            int expires = doc?.expires_in ?? 300;
            _tokenExpires = DateTime.UtcNow.AddSeconds(expires);
            return _accessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting Salesforce token");
            return null;
        }
    }

    public async Task<(bool ok, string? externalId)> CreateOrderAsync(OrderCreatedEvent evt)
    {
        if (evt == null) return (false, null);
        if (_processed.ContainsKey(evt.EventId))
        {
            _logger.LogInformation("Event {EventId} already processed", evt.EventId);
            return (true, null);
        }

        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token)) return (false, null);

        var apiBase = _config["Salesforce:ApiBaseUrl"];
        var ordersEndpoint = _config["Salesforce:OrdersEndpoint"] ?? "";

        if (string.IsNullOrEmpty(apiBase) || string.IsNullOrEmpty(ordersEndpoint))
        {
            _logger.LogError("Salesforce API endpoint not configured");
            return (false, null);
        }

        try
        {
            var client = _httpFactory.CreateClient("salesforce");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // simple mapping - adapt to real Salesforce fields
            var payload = new
            {
                OrderNumber = evt.OrderId,
                AccountId = evt.CustomerId,
                OrderDate = evt.OrderDate,
                Items = evt.Items
            };
            var json = JsonConvert.SerializeObject(payload);
            var resp = await client.PostAsync(ordersEndpoint, new StringContent(json, Encoding.UTF8, "application/json"));
            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                dynamic doc = JsonConvert.DeserializeObject(body);
                string? id = doc?.id ?? doc?.Id ?? null;
                _processed[evt.EventId] = true;
                _logger.LogInformation("Order {OrderId} created in Salesforce, id={Id}", evt.OrderId, id);
                return (true, id);
            }
            else
            {
                _logger.LogError("Salesforce create order failed: {Status} - {Body}", resp.StatusCode, await resp.Content.ReadAsStringAsync());
                return (false, null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending order to Salesforce");
            return (false, null);
        }
    }
}
