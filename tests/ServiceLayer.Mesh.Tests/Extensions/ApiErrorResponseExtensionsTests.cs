using ServiceLayer.Mesh.Extensions;

namespace ServiceLayer.Mesh.Tests.Extensions;

using Xunit;
using NHS.MESH.Client.Models;

public class ApiErrorResponseExtensionsTests
{
    [Fact]
    public void ToFormattedString_ReturnsFormattedString_WhenAllFieldsArePresent()
    {
        var error = new APIErrorResponse
        {
            ErrorEvent = "SomeEvent",
            ErrorCode = "123",
            ErrorDescription = "Something went awry"
        };

        var result = error.ToFormattedString();

        Assert.Equal("ErrorEvent: SomeEvent, ErrorCode: 123, ErrorDescription: Something went awry", result);
    }

    [Fact]
    public void ToFormattedString_HandlesNullErrorEvent()
    {
        var error = new APIErrorResponse
        {
            ErrorEvent = null,
            ErrorCode = "123",
            ErrorDescription = "Something went awry"
        };

        var result = error.ToFormattedString();

        Assert.Equal("ErrorEvent: N/A, ErrorCode: 123, ErrorDescription: Something went awry", result);
    }

    [Fact]
    public void ToFormattedString_HandlesNullErrorCode()
    {
        var error = new APIErrorResponse
        {
            ErrorEvent = "SomeEvent",
            ErrorCode = null,
            ErrorDescription = "Something went awry"
        };

        var result = error.ToFormattedString();

        Assert.Equal("ErrorEvent: SomeEvent, ErrorCode: N/A, ErrorDescription: Something went awry", result);
    }

    [Fact]
    public void ToFormattedString_HandlesNullErrorDescription()
    {
        var error = new APIErrorResponse
        {
            ErrorEvent = "SomeEvent",
            ErrorCode = "123",
            ErrorDescription = null!
        };

        var result = error.ToFormattedString();

        Assert.Equal("ErrorEvent: SomeEvent, ErrorCode: 123, ErrorDescription: ", result);
    }

    [Fact]
    public void ToFormattedString_HandlesAllFieldsNull()
    {
        var error = new APIErrorResponse();

        var result = error.ToFormattedString();

        Assert.Equal("ErrorEvent: N/A, ErrorCode: N/A, ErrorDescription: ", result);
    }

    [Fact]
    public void ToFormattedString_ReturnsFallback_WhenErrorIsNull()
    {
        APIErrorResponse? error = null;

        var result = error.ToFormattedString();

        Assert.Equal("Unknown error", result);
    }
}
