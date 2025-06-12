using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace ServiceLayer.Mesh.Functions;

public static class HealthCheckFunction
{
    [Function("HealthCheckFunction")]
    public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        return req.CreateResponse(HttpStatusCode.OK);
    }
}
