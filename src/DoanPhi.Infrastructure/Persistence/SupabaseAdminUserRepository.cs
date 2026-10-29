using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoanPhi.Infrastructure.Persistence;

public class SupabaseAdminUserDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("last_login")]
    public DateTime? LastLogin { get; set; }

    public AdminUser ToDomain()
    {
        return new AdminUser(Id, Username, PasswordHash, FullName, CreatedAt, LastLogin);
    }

    public static SupabaseAdminUserDto FromDomain(AdminUser user)
    {
        return new SupabaseAdminUserDto
        {
            Id = user.Id,
            Username = user.Username,
            PasswordHash = user.PasswordHash,
            FullName = user.FullName,
            CreatedAt = user.CreatedAt == default ? DateTime.UtcNow : user.CreatedAt,
            LastLogin = user.LastLogin
        };
    }
}

public class SupabaseAdminUserRepository : IAdminUserRepository
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseAdminUserRepository> _logger;

    private static readonly Lock _fallbackLock = new();
    private static readonly List<AdminUser> _fallbackUsers = [];

    public SupabaseAdminUserRepository(
        HttpClient httpClient,
        IOptions<SupabaseOptions> options,
        ILogger<SupabaseAdminUserRepository> logger)
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

    public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var cleanUsername = username.Trim().ToLowerInvariant();

        if (_options.IsConfigured)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"rest/v1/admin_users?username=eq.{Uri.EscapeDataString(cleanUsername)}&select=*", 
                    cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var dtos = await response.Content.ReadFromJsonAsync<List<SupabaseAdminUserDto>>(cancellationToken: cancellationToken);
                    var found = dtos?.FirstOrDefault();
                    if (found != null)
                    {
                        var domain = found.ToDomain();
                        lock (_fallbackLock)
                        {
                            _fallbackUsers.RemoveAll(u => u.Username == domain.Username);
                            _fallbackUsers.Add(domain);
                        }
                        return domain;
                    }
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Supabase GetByUsernameAsync error ({StatusCode}): {Error}", response.StatusCode, err);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch admin user '{Username}' from Supabase. Falling back to local cache.", cleanUsername);
            }
        }

        lock (_fallbackLock)
        {
            return _fallbackUsers.FirstOrDefault(u => u.Username == cleanUsername);
        }
    }

    public async Task<AdminUser> CreateAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        lock (_fallbackLock)
        {
            _fallbackUsers.RemoveAll(u => u.Username == user.Username);
            _fallbackUsers.Add(user);
        }

        if (_options.IsConfigured)
        {
            try
            {
                var dto = SupabaseAdminUserDto.FromDomain(user);
                using var request = new HttpRequestMessage(HttpMethod.Post, "rest/v1/admin_users")
                {
                    Content = JsonContent.Create(dto)
                };
                request.Headers.Add("Prefer", "return=representation");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var inserted = await response.Content.ReadFromJsonAsync<List<SupabaseAdminUserDto>>(cancellationToken: cancellationToken);
                    if (inserted != null && inserted.Count > 0)
                    {
                        return inserted[0].ToDomain();
                    }
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Supabase CreateAdmin error ({StatusCode}): {Error}", response.StatusCode, err);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create admin user '{Username}' in Supabase.", user.Username);
            }
        }

        return user;
    }

    public async Task UpdateAsync(AdminUser user, CancellationToken cancellationToken = default)
    {
        lock (_fallbackLock)
        {
            _fallbackUsers.RemoveAll(u => u.Username == user.Username);
            _fallbackUsers.Add(user);
        }

        if (_options.IsConfigured)
        {
            try
            {
                var dto = new
                {
                    password_hash = user.PasswordHash,
                    last_login = user.LastLogin
                };

                using var request = new HttpRequestMessage(HttpMethod.Patch, $"rest/v1/admin_users?username=eq.{Uri.EscapeDataString(user.Username)}")
                {
                    Content = JsonContent.Create(dto)
                };
                request.Headers.Add("Prefer", "return=minimal");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Supabase UpdateAdmin error ({StatusCode}): {Error}", response.StatusCode, err);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update admin user '{Username}' in Supabase.", user.Username);
            }
        }
    }

    public async Task<bool> HasAnyAdminAsync(CancellationToken cancellationToken = default)
    {
        if (_options.IsConfigured)
        {
            try
            {
                var response = await _httpClient.GetAsync("rest/v1/admin_users?select=id&limit=1", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var list = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>(cancellationToken: cancellationToken);
                    if (list != null && list.Count > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check if any admin exists in Supabase.");
            }
        }

        lock (_fallbackLock)
        {
            return _fallbackUsers.Count > 0;
        }
    }
}
