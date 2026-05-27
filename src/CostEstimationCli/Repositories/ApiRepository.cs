using System.Text;
using CostEstimationCli.Configuration;
using CostEstimationCli.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CostEstimationCli.Repositories;

/// <summary>
/// Repository implementation for API communication
/// </summary>
public class ApiRepository : IApiRepository
{
    private readonly HttpClient _httpClient;
    private readonly CostEstimationSettings _settings;
    private readonly ILogger<ApiRepository> _logger;

    public ApiRepository(
        HttpClient httpClient,
        IOptions<CostEstimationSettings> settings,
        ILogger<ApiRepository> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Configure HTTP client with API key if authentication is enabled
        if (_settings.Authentication.Enabled && !string.IsNullOrEmpty(_settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", _settings.ApiKey);
        }
    }

    /// <inheritdoc />
    public async Task<CostEstimateResponseModel> GetCostEstimateAsync(
        string pulumiPreviewJson,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pulumiPreviewJson))
        {
            throw new ArgumentException("Pulumi preview JSON cannot be empty", nameof(pulumiPreviewJson));
        }

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            throw new InvalidOperationException("Base URL is not configured");
        }

        try
        {
            _logger.LogInformation("Sending cost estimation request to {BaseUrl}", _settings.BaseUrl);

            var content = new StringContent(pulumiPreviewJson, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_settings.BaseUrl, content, cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var costEstimate = JsonConvert.DeserializeObject<CostEstimateResponseModel>(responseString);

            if (costEstimate == null)
            {
                throw new InvalidOperationException("Failed to deserialize cost estimate response");
            }

            _logger.LogInformation("Cost estimation received successfully");
            return costEstimate;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting cost estimate");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize API response");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<LicenseResponseModel> ValidateLicenseAsync(
        string apiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be empty", nameof(apiKey));
        }

        try
        {
            _logger.LogInformation("Validating license");

            var licenseUrl = $"{_settings.BaseUrl}/licensing?api_key={apiKey}";
            var response = await _httpClient.GetAsync(licenseUrl, cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
            var licenseResponse = JsonConvert.DeserializeObject<LicenseResponseModel>(responseString);

            if (licenseResponse == null)
            {
                throw new InvalidOperationException("Failed to deserialize license response");
            }

            _logger.LogInformation("License validation completed: {IsAuthorized}", licenseResponse.IsAuthorized);
            return licenseResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while validating license");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize license response");
            throw;
        }
    }
}
