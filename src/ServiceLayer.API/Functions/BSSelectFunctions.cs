using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ServiceLayer.API.Models;
using ServiceLayer.API.Shared;

namespace ServiceLayer.API.Functions;

public class BSSelectFunctions(ILogger<BSSelectFunctions> logger, EventGridPublisherClient eventGridPublisherClient)
{
    [Function("CreateEpisodeEvent")]
    public async Task<IActionResult> CreateEpisodeEvent([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "bsselect/episodes")] HttpRequest req)
    {
        BSSelectEpisodeEvent? bssEpisodeEvent;

        try
        {
            bssEpisodeEvent = await JsonSerializer.DeserializeAsync<BSSelectEpisodeEvent>(req.Body);

            if (bssEpisodeEvent == null)
            {
                return new BadRequestObjectResult("Deserialization resulted in null.");
            }

            var validationResults = new List<ValidationResult>();
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
                NhsNumber = bssEpisodeEvent.NhsNumber,
                DOB = bssEpisodeEvent.DateOfBirth,
                Name = $"{bssEpisodeEvent.FirstGivenName} {bssEpisodeEvent.FamilyName}",
                ScreeningName = "Breast Screening",
                PathwayTypeName = "Breast Screening Routine"
            };

            var cloudEvent = new CloudEvent(
                "ServiceLayer",
                "CreateBrestScreeningPathwayEnrolment",
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
            logger.LogError(ex, "Failed to send CreateBrestScreeningPathwayEnrolment event");
            return new StatusCodeResult(500);
        }
    }
}
