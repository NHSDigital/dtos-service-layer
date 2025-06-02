using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace ServiceLayer.Mesh.Functions;

public class HealthCheckFunction
{
    [Function("HealthCheck")]
    public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
    {
        return req.CreateResponse(HttpStatusCode.OK);
    }
}
