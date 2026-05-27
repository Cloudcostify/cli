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
}
