using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoanPhi.Infrastructure.Persistence;

public class SupabaseSystemSettingDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; } = 1;

    [JsonPropertyName("is_form_open")]
    public bool IsFormOpen { get; set; } = true;

    [JsonPropertyName("close_reason")]
    public string? CloseReason { get; set; }

    [JsonPropertyName("last_updated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public SystemSetting ToDomain()
    {
        return new SystemSetting(IsFormOpen, CloseReason, LastUpdated);
    }
}

public class SupabaseSystemSettingRepository : ISystemSettingRepository
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseSystemSettingRepository> _logger;

    private static readonly Lock _lock = new();
    private static SystemSetting _fallbackSetting = SystemSetting.CreateDefault();

    public SupabaseSystemSettingRepository(
        HttpClient httpClient,
        IOptions<SupabaseOptions> options,
        ILogger<SupabaseSystemSettingRepository> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        if (_options.IsConfigured && _httpClient.BaseAddress == null)
        {
            var baseUrl = _options.Url.TrimEnd('/') + "/";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", _options.AnonKey);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AnonKey);
        }
    }

    public async Task<SystemSetting> GetSettingAsync(CancellationToken cancellationToken = default)
    {
        if (_options.IsConfigured)
        {
            try
            {
                var response = await _httpClient.GetAsync("rest/v1/system_settings?id=eq.1&select=*", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var list = await response.Content.ReadFromJsonAsync<List<SupabaseSystemSettingDto>>(cancellationToken: cancellationToken);
                    var item = list?.FirstOrDefault();
                    if (item != null)
                    {
                        var domain = item.ToDomain();
                        lock (_lock)
                        {
                            _fallbackSetting = domain;
                        }
                        return domain;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load system settings from Supabase. Using fallback.");
            }
        }

        lock (_lock)
        {
            return _fallbackSetting;
        }
    }

    public async Task UpdateSettingAsync(SystemSetting setting, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _fallbackSetting = setting;
        }

        if (_options.IsConfigured)
        {
            try
            {
                var dto = new
                {
                    is_form_open = setting.IsFormOpen,
                    close_reason = setting.CloseReason,
                    last_updated = setting.LastUpdated
                };

                using var request = new HttpRequestMessage(HttpMethod.Patch, "rest/v1/system_settings?id=eq.1")
                {
                    Content = JsonContent.Create(dto)
                };
                request.Headers.Add("Prefer", "return=representation");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Supabase UpdateSetting Error ({StatusCode}): {Error}", response.StatusCode, err);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update system setting in Supabase.");
            }
        }
    }
}
