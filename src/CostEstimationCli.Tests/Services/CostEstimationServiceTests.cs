using CostEstimationCli.Models;
using CostEstimationCli.Repositories;
using CostEstimationCli.Services;
using CostEstimationCli.Services.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TUnit.Core;

namespace CostEstimationCli.Tests.Services;

public class CostEstimationServiceTests
{
    private readonly IInfrastructureProvider _provider;
    private readonly IApiRepository _apiRepository;
    private readonly ILogger<CostEstimationService> _logger;

    public CostEstimationServiceTests()
    {
        _provider = Substitute.For<IInfrastructureProvider>();
        _apiRepository = Substitute.For<IApiRepository>();
        _logger = Substitute.For<ILogger<CostEstimationService>>();
    }

    [Test]
    public void Constructor_WithNullProvider_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new CostEstimationService(null!, _apiRepository, _logger);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("provider");
    }

    [Test]
    public void Constructor_WithNullApiRepository_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new CostEstimationService(_provider, null!, _logger);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiRepository");
    }

    [Test]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new CostEstimationService(_provider, _apiRepository, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Test]
    public async Task EstimateCostAsync_WithValidData_ShouldReturnCostEstimate()
    {
        // Arrange
        var infrastructureJson = "{\"test\": \"data\"}";
        var expectedCostEstimate = new CostEstimateResponseModel
        {
            currency = "USD",
            cloudProvider = "Azure",
            aggregateCosts = new AggregateCost { PerHour = 10.50m }
        };

        _provider.ExtractResourceJsonAsync(Arg.Any<CancellationToken>())
            .Returns(infrastructureJson);

        _apiRepository.GetCostEstimateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(expectedCostEstimate);

        var service = new CostEstimationService(_provider, _apiRepository, _logger);

        // Act
        var result = await service.EstimateCostAsync();

        // Assert
        result.Should().NotBeNull();
        result.currency.Should().Be("USD");
        result.cloudProvider.Should().Be("Azure");
        result.aggregateCosts.PerHour.Should().Be(10.50m);

        await _provider.Received(1).ExtractResourceJsonAsync(Arg.Any<CancellationToken>());
        await _apiRepository.Received(1).GetCostEstimateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task EstimateCostAsync_WhenProviderFails_ShouldThrowException()
    {
        // Arrange
        _provider.ExtractResourceJsonAsync(Arg.Any<CancellationToken>())
            .Returns<string>(_ => throw new InvalidOperationException("Provider error"));

        var service = new CostEstimationService(_provider, _apiRepository, _logger);

        // Act & Assert
        var act = async () => await service.EstimateCostAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Provider error");
    }

    [Test]
    public async Task EstimateCostAsync_WhenApiRepositoryFails_ShouldThrowException()
    {
        // Arrange
        var infrastructureJson = "{\"test\": \"data\"}";

        _provider.ExtractResourceJsonAsync(Arg.Any<CancellationToken>())
            .Returns(infrastructureJson);

        _apiRepository.GetCostEstimateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<CostEstimateResponseModel>(_ => throw new HttpRequestException("API error"));

        var service = new CostEstimationService(_provider, _apiRepository, _logger);

        // Act & Assert
        var act = async () => await service.EstimateCostAsync();
        await act.Should().ThrowAsync<HttpRequestException>()
            .WithMessage("API error");
    }
}
