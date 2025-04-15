using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace TestEventGridSubscriber;

public class TestEventGridSubscriber
{
    public static List<CloudEvent> EventStore { get; } = [];

    [Function("EventHandler")]
    public static void Run([EventGridTrigger] CloudEvent cloudEvent)
    {
        EventStore.Add(cloudEvent);
    }

    [Function("ClearEventsStore")]
    public static IActionResult ClearEventsStore(
    [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "events")] HttpRequestData req)
    {
        EventStore.Clear();
        return new NoContentResult();
    }

    [Function("GetEvents")]
    public static IActionResult GetEvents(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "events")] HttpRequestData req)
    {
        return new OkObjectResult(EventStore);
    }
}
