using System.Net;
using CostEstimationCli.Configuration;
using CostEstimationCli.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using TUnit.Core;

namespace CostEstimationCli.Tests.Repositories;

public class ApiRepositoryTests
{
    private readonly HttpClient _httpClient;
    private readonly IOptions<CostEstimationSettings> _settings;
    private readonly ILogger<ApiRepository> _logger;

    public ApiRepositoryTests()
    {
        _httpClient = new HttpClient();
        _settings = Substitute.For<IOptions<CostEstimationSettings>>();
        _logger = Substitute.For<ILogger<ApiRepository>>();
    }

    [Test]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ApiRepository(null!, _settings, _logger);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Test]
    public void Constructor_WithNullSettings_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ApiRepository(_httpClient, null!, _logger);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settings");
    }

    [Test]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new ApiRepository(_httpClient, _settings, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Test]
    public async Task GetCostEstimateAsync_WithEmptyJson_ShouldThrowArgumentException()
    {
        // Arrange
        _settings.Value.Returns(new CostEstimationSettings
        {
            BaseUrl = "http://localhost",
            Authentication = new AuthenticationSettings { Enabled = false }
        });

        var repository = new ApiRepository(_httpClient, _settings, _logger);

        // Act & Assert
        var act = async () => await repository.GetCostEstimateAsync("");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("pulumiPreviewJson");
    }

    [Test]
    public async Task GetCostEstimateAsync_WithEmptyBaseUrl_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _settings.Value.Returns(new CostEstimationSettings
        {
            BaseUrl = "",
            Authentication = new AuthenticationSettings { Enabled = false }
        });

        var repository = new ApiRepository(_httpClient, _settings, _logger);

        // Act & Assert
        var act = async () => await repository.GetCostEstimateAsync("{\"test\": \"data\"}");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Base URL*");
    }

    [Test]
    public async Task ValidateLicenseAsync_WithEmptyApiKey_ShouldThrowArgumentException()
    {
        // Arrange
        _settings.Value.Returns(new CostEstimationSettings
        {
            BaseUrl = "http://localhost",
            Authentication = new AuthenticationSettings { Enabled = false }
        });

        var repository = new ApiRepository(_httpClient, _settings, _logger);

        // Act & Assert
        var act = async () => await repository.ValidateLicenseAsync("");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("apiKey");
    }

    [Test]
    public async Task GetCostEstimateAsync_SendsDashboardKeyToCostEstimateEndpoint()
    {
        var handler = new RecordingHandler();
        using var client = new HttpClient(handler);
        _settings.Value.Returns(new CostEstimationSettings
        {
            BaseUrl = "https://api.cloudcostify.io/costestimation/costestimate",
            ApiKey = "cc_live_test",
            Authentication = new AuthenticationSettings { Enabled = true }
        });

        var repository = new ApiRepository(client, _settings, _logger);
        var act = async () => await repository.GetCostEstimateAsync("{}");

        await act.Should().ThrowAsync<HttpRequestException>();
        handler.RequestUri.Should().Be("https://api.cloudcostify.io/costestimation/costestimate");
        handler.ApiKey.Should().Be("cc_live_test");
        handler.HasAuthorizationHeader.Should().BeFalse();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public string? RequestUri { get; private set; }
        public string? ApiKey { get; private set; }
        public bool HasAuthorizationHeader { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri?.ToString();
            ApiKey = request.Headers.TryGetValues("X-API-Key", out var values)
                ? values.SingleOrDefault()
                : null;
            HasAuthorizationHeader = request.Headers.Authorization is not null;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        }
    }
}
