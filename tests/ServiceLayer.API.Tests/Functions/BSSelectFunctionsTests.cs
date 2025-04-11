using System.Dynamic;
using System.Globalization;
using Azure;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ServiceLayer.API.Functions;
using ServiceLayer.API.Shared;
using ServiceLayer.API.Tests.Utils;

namespace ServiceLayer.API.Tests.Functions;

public class BSSelectFunctionsTests
{
    private readonly Mock<ILogger<BSSelectFunctions>> _logger = new();
    private readonly Mock<EventGridPublisherClient> _mockEventGridPublisherClient = new();
    private readonly BSSelectFunctions _functions;
    private readonly SetupRequest _setupRequest = new();
    private readonly dynamic _episode = new ExpandoObject();
    public static TheoryData<string> RequiredPropertyNames =>
    [
        "episode_id",
        "nhs_number",
        "date_of_birth",
        "first_given_name",
        "family_name"
    ];

    public BSSelectFunctionsTests()
    {
        _functions = new BSSelectFunctions(_logger.Object, _mockEventGridPublisherClient.Object);

        // Configuring a valid episode
        _episode.episode_id = "123";
        _episode.nhs_number = "9990000000";
        _episode.date_of_birth = "1970-01-01";
        _episode.first_given_name = "Test";
        _episode.family_name = "User";
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldSendEventAndReturnOk_WhenRequestIsValid()
    {
        // Arrange
        var request = _setupRequest.CreateMockHttpRequest(_episode);
        var mockResponse = Mock.Of<Response>(r => r.IsError == false);
        CloudEvent? capturedEvent = null;
        _mockEventGridPublisherClient.Setup(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()))
            .Callback<CloudEvent, CancellationToken>((ce, _) =>
            {
                capturedEvent = ce;
            })
            .ReturnsAsync(mockResponse);

        // Act
        var response = await _functions.IngressEpisode(request);

        // Assert
        Assert.IsType<OkResult>(response);
        _mockEventGridPublisherClient.Verify(x =>
            x.SendEventAsync(It.Is<CloudEvent>(ce =>
                ce.Type == "EpisodeEvent" &&
                ce.Source == "ServiceLayer" &&
                ce.Data != null
            ), default), Times.Once());

        var data = capturedEvent!.Data!.ToObjectFromJson<CreatePathwayParticipantDto>();
        Assert.NotNull(data);
        Assert.Equal(new Guid("11111111-1111-1111-1111-111111111113"), data.PathwayTypeId);
        Assert.Equal("Breast Screening Routine", data.PathwayTypeName);
        Assert.Equal("Breast Screening", data.ScreeningName);
        Assert.Equal(_episode.nhs_number, data.NhsNumber);
        Assert.Equal(DateOnly.Parse(_episode.date_of_birth, CultureInfo.CurrentCulture), data.DOB);
        Assert.Equal($"{_episode.first_given_name} {_episode.family_name}", data.Name);
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenRequestBodyEmpty()
    {
        // Arrange
        var request = _setupRequest.CreateMockHttpRequest(null);

        // Act
        var response = await _functions.IngressEpisode(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("Deserialization returned null", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [MemberData(nameof(RequiredPropertyNames))]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenRequiredPropertyIsMissing(string propertyName)
    {
        // Arrange
        ((IDictionary<string, object?>)_episode).Remove(propertyName);
        var request = _setupRequest.CreateMockHttpRequest(_episode);

        // Act
        var response = await _functions.IngressEpisode(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal($"{propertyName} is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [MemberData(nameof(RequiredPropertyNames))]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenRequiredPropertyIsNull(string propertyName)
    {
        // Arrange
        ((IDictionary<string, object?>)_episode)[propertyName] = null;
        var request = _setupRequest.CreateMockHttpRequest(_episode);

        // Act
        var response = await _functions.IngressEpisode(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal($"{propertyName} is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [MemberData(nameof(RequiredPropertyNames))]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenRequiredPropertyIsEmptyString(string propertyName)
    {
        // Arrange
        ((IDictionary<string, object?>)_episode)[propertyName] = "";
        var request = _setupRequest.CreateMockHttpRequest(_episode);

        // Act
        var response = await _functions.IngressEpisode(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal($"{propertyName} is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [MemberData(nameof(RequiredPropertyNames))]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenRequiredPropertyIsWhitespace(string propertyName)
    {
        // Arrange
        ((IDictionary<string, object?>)_episode)[propertyName] = " ";
        var request = _setupRequest.CreateMockHttpRequest(_episode);

        // Act
        var response = await _functions.IngressEpisode(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal($"{propertyName} is required", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [InlineData("ABCDEFGHIJ")]
    [InlineData("999999999")]
    [InlineData("10000000000")]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenNhsNumberIsInvalidValue(string? nhsNumber)
    {
        // Arrange
        _episode.nhs_number = nhsNumber;
        var request = _setupRequest.CreateMockHttpRequest(_episode);

        // Act
        var response = await _functions.IngressEpisode(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("nhs_number must be exactly 10 digits", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("123")]
    public async Task CreateEpisodeEvent_ShouldReturnBadRequest_WhenDateOfBirthIsInvalidValue(string? dateOfBirth)
    {
        // Arrange
        _episode.date_of_birth = dateOfBirth;
        var request = _setupRequest.CreateMockHttpRequest(_episode);

        // Act
        var response = await _functions.IngressEpisode(request);

        // Assert
        var result = Assert.IsType<BadRequestObjectResult>(response);
        Assert.Equal("date_of_birth is invalid", result.Value);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldReturnInternalServerError_WhenEventFailsToSend()
    {
        // Arrange
        var request = _setupRequest.CreateMockHttpRequest(_episode);
        var mockResponse = Mock.Of<Response>(r => r.IsError == true);
        _mockEventGridPublisherClient.Setup(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockResponse);

        // Act
        var response = await _functions.IngressEpisode(request);

        // Assert
        var result = Assert.IsType<StatusCodeResult>(response);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), default), Times.Once());
    }

    [Fact]
    public async Task CreateEpisodeEvent_ShouldReturnInternalServerError_WhenSendEventThrowsException()
    {
        // Arrange
        var request = _setupRequest.CreateMockHttpRequest(_episode);
        var mockResponse = Mock.Of<Response>(r => r.IsError == true);
        _mockEventGridPublisherClient.Setup(x => x.SendEventAsync(It.IsAny<CloudEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException("Failed to send event to Event Grid"));

        // Act
        var response = await _functions.IngressEpisode(request);

        // Assert
        var result = Assert.IsType<StatusCodeResult>(response);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        _mockEventGridPublisherClient.Verify(x => x.SendEventAsync(It.IsAny<CloudEvent>(), default), Times.Once());
    }
}
