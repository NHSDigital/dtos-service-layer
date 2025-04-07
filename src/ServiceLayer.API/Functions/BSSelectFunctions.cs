using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ServiceLayer.API.Models;
using ServiceLayer.API.Shared;

namespace ServiceLayer.API.Functions;

public class BSSelectFunctions(ILogger<BSSelectFunctions> logger, EventGridPublisherClient eventGridPublisherClient)
{
    [Function("BSSelectIngressEpisode")]
    public async Task<IActionResult> IngressEpisode([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bsselect/episodes/ingress")] HttpRequestData req)
    {
        BSSelectEpisode? bssEpisodeEvent;

        try
        {
            bssEpisodeEvent = await JsonSerializer.DeserializeAsync<BSSelectEpisode>(req.Body);

            if (bssEpisodeEvent == null)
            {
                logger.LogError("Deserialization returned null");
                return new BadRequestObjectResult("Deserialization returned null");
            }

            var validationContext = new ValidationContext(bssEpisodeEvent);

            Validator.ValidateObject(bssEpisodeEvent, validationContext, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured when reading request body");
            return new BadRequestObjectResult(ex.Message);
        }

        try
        {
            var createPathwayEnrolment = new CreatePathwayParticipantDto
            {
                PathwayTypeId = new Guid("11111111-1111-1111-1111-111111111113"),
                PathwayTypeName = "Breast Screening Routine",
                ScreeningName = "Breast Screening",
                NhsNumber = bssEpisodeEvent.NhsNumber!,
                DOB = DateOnly.Parse(bssEpisodeEvent.DateOfBirth!),
                Name = $"{bssEpisodeEvent.FirstGivenName} {bssEpisodeEvent.FamilyName}",
            };

            var cloudEvent = new CloudEvent(
                "ServiceLayer",
                "EpisodeEvent",
                createPathwayEnrolment
            );

            var response = await eventGridPublisherClient.SendEventAsync(cloudEvent);

            if (response.IsError)
            {
                logger.LogError(
                    "Failed to send event to Event Grid.\nSource: {source}\nType: {type}\n Response status code: {code}",
                    cloudEvent.Source, cloudEvent.Type, response.Status);
                return new StatusCodeResult(500);
            }

            return new OkResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send event to Event Grid");
            return new StatusCodeResult(500);
        }
    }
}
