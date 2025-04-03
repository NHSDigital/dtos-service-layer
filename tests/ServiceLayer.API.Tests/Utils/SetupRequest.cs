using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Moq;

namespace ServiceLayer.API.Tests.Utils;

public class SetupRequest
{
    private readonly Mock<FunctionContext> _context;

    public SetupRequest()
    {
        _context = new Mock<FunctionContext>();
    }

    /// <summary>
    /// Creates a mock HTTP request with a JSON body
    /// </summary>
    /// <param name="body">The object to serialize as JSON</param>
    /// <returns>A mock HttpRequestData</returns>
    public HttpRequestData CreateMockHttpRequest(object? body)
    {
        var json = JsonSerializer.Serialize(body);
        var byteArray = Encoding.UTF8.GetBytes(json);
        var memoryStream = new MemoryStream(byteArray);
        var mockRequest = new Mock<HttpRequestData>(MockBehavior.Strict, _context.Object);
        mockRequest.Setup(r => r.Body).Returns(memoryStream);
        return mockRequest.Object;
    }
}
