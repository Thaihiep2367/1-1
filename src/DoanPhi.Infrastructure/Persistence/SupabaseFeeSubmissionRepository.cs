using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DoanPhi.Domain.Entities;
using DoanPhi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DoanPhi.Infrastructure.Persistence;

public class SupabaseFeeSubmissionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("student_id")]
    public string StudentId { get; set; } = string.Empty;

    [JsonPropertyName("membership_status")]
    public string MembershipStatus { get; set; } = string.Empty;

    [JsonPropertyName("duration_months")]
    public int DurationMonths { get; set; } = 12;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("transfer_memo")]
    public string TransferMemo { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("proof_image")]
    public string? ProofImage { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    public FeeSubmission ToDomain()
    {
        return new FeeSubmission(
            Id,
            FullName,
            StudentId,
            MembershipStatus,
            DurationMonths,
            Amount,
            TransferMemo,
            Notes,
            ProofImage,
            CreatedAt
        );
    }

    public static SupabaseFeeSubmissionDto FromDomain(FeeSubmission submission)
    {
        return new SupabaseFeeSubmissionDto
        {
            Id = submission.Id,
            FullName = submission.FullName,
            StudentId = submission.StudentId,
            MembershipStatus = submission.MembershipStatus,
            DurationMonths = submission.DurationMonths,
            Amount = submission.Amount,
            TransferMemo = submission.TransferMemo,
            Notes = submission.Notes,
            ProofImage = submission.ProofImage,
            CreatedAt = submission.CreatedAt == default ? DateTime.UtcNow : submission.CreatedAt
        };
    }
}

public class SupabaseFeeSubmissionRepository : IFeeSubmissionRepository
{
    private readonly HttpClient _httpClient;
    private readonly SupabaseOptions _options;
    private readonly ILogger<SupabaseFeeSubmissionRepository> _logger;

    // Fallback cache nội bộ
    private static readonly Lock _fallbackLock = new();
    private static readonly List<FeeSubmission> _fallbackCache = [];

    public SupabaseFeeSubmissionRepository(
        HttpClient httpClient,
        IOptions<SupabaseOptions> options,
        ILogger<SupabaseFeeSubmissionRepository> logger)
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

    public async Task<IReadOnlyList<FeeSubmission>> GetAllAsync(string? search = null, CancellationToken cancellationToken = default)
    {
        if (_options.IsConfigured)
        {
            try
            {
                var queryUrl = "rest/v1/fee_submissions?select=*&order=created_at.desc";
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var term = Uri.EscapeDataString(search.Trim());
                    queryUrl += $"&or=(full_name.ilike.*{term}*,student_id.ilike.*{term}*,transfer_memo.ilike.*{term}*)";
                }

                var response = await _httpClient.GetAsync(queryUrl, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var dtos = await response.Content.ReadFromJsonAsync<List<SupabaseFeeSubmissionDto>>(cancellationToken: cancellationToken);
                    if (dtos != null)
                    {
                        var domainList = dtos.Select(d => d.ToDomain()).ToList();
                        lock (_fallbackLock)
                        {
                            _fallbackCache.Clear();
                            _fallbackCache.AddRange(domainList);
                        }
                        return domainList;
                    }
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Supabase GetAllAsync returned {StatusCode}: {Error}. Falling back to memory cache.", response.StatusCode, err);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch fee submissions from Supabase. Falling back to memory cache.");
            }
        }

        // Fallback to local memory cache
        lock (_fallbackLock)
        {
            IEnumerable<FeeSubmission> query = _fallbackCache;
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLowerInvariant();
                query = query.Where(s =>
                    s.FullName.ToLowerInvariant().Contains(term) ||
                    s.StudentId.ToLowerInvariant().Contains(term) ||
                    s.TransferMemo.ToLowerInvariant().Contains(term));
            }
            return query.OrderByDescending(s => s.CreatedAt).ToList();
        }
    }

    public async Task<FeeSubmission?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_options.IsConfigured)
        {
            try
            {
                var response = await _httpClient.GetAsync($"rest/v1/fee_submissions?id=eq.{id}&select=*", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var dtos = await response.Content.ReadFromJsonAsync<List<SupabaseFeeSubmissionDto>>(cancellationToken: cancellationToken);
                    var found = dtos?.FirstOrDefault();
                    if (found != null) return found.ToDomain();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get submission {Id} from Supabase.", id);
            }
        }

        lock (_fallbackLock)
        {
            return _fallbackCache.FirstOrDefault(s => s.Id == id);
        }
    }

    public async Task<FeeSubmission> AddAsync(FeeSubmission submission, CancellationToken cancellationToken = default)
    {
        // 1. Luôn cập nhật local fallback cache trước để đảm bảo dữ liệu không bị mất
        lock (_fallbackLock)
        {
            _fallbackCache.RemoveAll(s => s.Id == submission.Id);
            _fallbackCache.Insert(0, submission);
        }

        // 2. Gửi đồng bộ lên Supabase Cloud Database
        if (_options.IsConfigured)
        {
            try
            {
                var dto = SupabaseFeeSubmissionDto.FromDomain(submission);
                using var request = new HttpRequestMessage(HttpMethod.Post, "rest/v1/fee_submissions")
                {
                    Content = JsonContent.Create(dto)
                };
                request.Headers.Add("Prefer", "return=representation");

                var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully inserted fee submission {Id} to Supabase.", submission.Id);
                    var insertedList = await response.Content.ReadFromJsonAsync<List<SupabaseFeeSubmissionDto>>(cancellationToken: cancellationToken);
                    if (insertedList != null && insertedList.Count > 0)
                    {
                        return insertedList[0].ToDomain();
                    }
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Supabase Insert Error ({StatusCode}): {Error}. (Kiểm tra chính sách RLS trên Supabase).", response.StatusCode, err);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert submission {Id} into Supabase. Saved in local cache.", submission.Id);
            }
        }

        return submission;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_fallbackLock)
        {
            _fallbackCache.RemoveAll(s => s.Id == id);
        }

        if (_options.IsConfigured)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"rest/v1/fee_submissions?id=eq.{id}", cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Supabase Delete Error ({StatusCode}): {Error}", response.StatusCode, err);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete submission {Id} from Supabase.", id);
            }
        }
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        lock (_fallbackLock)
        {
            _fallbackCache.Clear();
        }

        if (_options.IsConfigured)
        {
            try
            {
                var response = await _httpClient.DeleteAsync("rest/v1/fee_submissions?id=not.is.null", cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var err = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Supabase ClearAll Error ({StatusCode}): {Error}", response.StatusCode, err);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear all submissions from Supabase.");
            }
        }
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(null, cancellationToken);
        return all.Count;
    }
}
