using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.API.Functions;
using ServiceLayer.API.Tests.Utils;

namespace ServiceLayer.API.Tests.Functions;

public class BSSelectFunctionsTests
{
    private readonly Mock<ILogger<BSSelectFunctions>> _logger = new();
    private readonly Mock<EventGridPublisherClient> _mockEventGridPublisherClient = new();
    private readonly BSSelectFunctions _functions;
    private readonly SetupRequest _setupRequest = new();

    public BSSelectFunctionsTests()
    {
        _functions = new BSSelectFunctions(_logger.Object, _mockEventGridPublisherClient.Object);
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldSendEventAndReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var episode = new
        {
            episode_id = "123",
            nhs_number = "9990000000",
            date_of_birth = "1970-01-01",
            first_given_name = "Test",
            family_name = "User",
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);
        var mockResponse = Mock.Of<Response>(r => r.IsError == false);
        _mockEventGridPublisherClient.Setup(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockResponse);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        Assert.IsType<OkResult>(response);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), default), Times.Once());
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenRequestBodyEmpty()
    {
        // Arrange
        var request = _setupRequest.CreateMockHttpRequest(null);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("Deserialization returned null", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenEpisodeIdIsMissing()
    {
        // Arrange
        var episode = new
        {
            nhs_number = "9990000000",
            date_of_birth = "1970-01-01",
            first_given_name = "Test",
            family_name = "User",
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("The episode_id is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenEpisodeIdIsEmptyValue(string? episodeId)
    {
        // Arrange
        var episode = new
        {
            episode_id = episodeId,
            nhs_number = "9990000000",
            date_of_birth = "1970-01-01",
            first_given_name = "Test",
            family_name = "User",
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("The episode_id is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenNhsNumberIsMissing()
    {
        // Arrange
        var episode = new
        {
            episode_id = "123",
            date_of_birth = "1970-01-01",
            first_given_name = "Test",
            family_name = "User",
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("The nhs_number is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenNhsNumberIsEmptyValue(string? nhsNumber)
    {
        // Arrange
        var episode = new
        {
            episode_id = "123",
            nhs_number = nhsNumber,
            date_of_birth = "1970-01-01",
            first_given_name = "Test",
            family_name = "User",
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("The nhs_number is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [InlineData("ABCDEFGHIJ")]
    [InlineData("999999999")]
    [InlineData("10000000000")]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenNhsNumberIsInvalidValue(string? nhsNumber)
    {
        // Arrange
        var episode = new
        {
            episode_id = "123",
            nhs_number = nhsNumber,
            date_of_birth = "1970-01-01",
            first_given_name = "Test",
            family_name = "User",
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("The nhs_number must be exactly 10 digits", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenDateOfBirthIsMissing()
    {
        // Arrange
        var episode = new
        {
            episode_id = "123",
            nhs_number = "9990000000",
            first_given_name = "Test",
            family_name = "User",
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("The date_of_birth is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [InlineData(null)]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenDateOfBirthIsEmptyValue(string? dateOfBirth)
    {
        // Arrange
        var episode = new
        {
            episode_id = "123",
            nhs_number = "9990000000",
            date_of_birth = dateOfBirth,
            first_given_name = "Test",
            family_name = "User",
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("The date_of_birth is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenFirstGivenNameIsMissing()
    {
        // Arrange
        var episode = new
        {
            episode_id = "123",
            nhs_number = "9990000000",
            date_of_birth = "1970-01-01",
            family_name = "User",
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("The first_given_name is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenFirstGivenNameIsEmptyValue(string? firstGivenName)
    {
        // Arrange
        var episode = new
        {
            episode_id = "123",
            nhs_number = "9990000000",
            date_of_birth = "1970-01-01",
            first_given_name = firstGivenName,
            family_name = "User",
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("The first_given_name is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenFamilyNameIsMissing()
    {
        // Arrange
        var episode = new
        {
            episode_id = "123",
            nhs_number = "9990000000",
            date_of_birth = "1970-01-01",
            first_given_name = "Test"
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("The family_name is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenFamilyNameIsEmptyValue(string? familyName)
    {
        // Arrange
        var episode = new
        {
            episode_id = "123",
            nhs_number = "9990000000",
            date_of_birth = "1970-01-01",
            first_given_name = "Test",
            family_name = familyName,
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("The family_name is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldReturnInternalServerError_WhenEventFailsToSend()
    {
        // Arrange
        var episode = new
        {
            episode_id = "123",
            nhs_number = "9990000000",
            date_of_birth = "1970-01-01",
            first_given_name = "Test",
            family_name = "User",
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);
        var mockResponse = Mock.Of<Response>(r => r.IsError == true);
        _mockEventGridPublisherClient.Setup(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockResponse);

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<StatusCodeResult>(response);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), default), Times.Once());
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldReturnInternalServerError_WhenSendEventThrowsException()
    {
        // Arrange
        var episode = new
        {
            episode_id = "123",
            nhs_number = "9990000000",
            date_of_birth = "1970-01-01",
            first_given_name = "Test",
            family_name = "User",
        };
        var request = _setupRequest.CreateMockHttpRequest(episode);
        var mockResponse = Mock.Of<Response>(r => r.IsError == true);
        _mockEventGridPublisherClient.Setup(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("Failed to send event to Event Grid"));

        // Act
        var response = await _functions.CreateEpisodeEvent(request);

        // Assert
        var result = Assert.IsType<StatusCodeResult>(response);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), default), Times.Once());
    }
}
