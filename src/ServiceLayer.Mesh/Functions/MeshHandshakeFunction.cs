using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Mesh.Configuration;

namespace ServiceLayer.Mesh.Functions
{
    public class MeshHandshakeFunction(
        ILogger<MeshHandshakeFunction> logger,
        IMeshOperationService meshOperationService,
        IMeshHandshakeFunctionConfiguration configuration)
    {
        [Function("MeshHandshakeFunction")]
        public async Task Run([TimerTrigger("%MeshHandshakeTimerExpression%")] TimerInfo myTimer)
        {
            logger.LogInformation("{FunctionName} started at: {Time}", nameof(MeshHandshakeFunction), DateTime.UtcNow);

            try
            {
                var response = await meshOperationService.MeshHandshakeAsync(configuration.NbssMeshMailboxId);

                if (response.IsSuccessful)
                {
                    logger.LogInformation("Mesh handshake completed successfully for mailbox {MailboxId} at {Time}. Status: {Status}",
                    response.Response.MailboxId, DateTime.UtcNow, response.IsSuccessful);
                }
                else
                {
                    logger.LogWarning("Mesh handshake failed for mailbox {MailboxId} at {Time}. Error: {Error}",
                    configuration.NbssMeshMailboxId, DateTime.UtcNow, response.Error);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred during mesh handshake for mailbox {MailboxId} at {Time}", configuration.NbssMeshMailboxId, DateTime.UtcNow);
                // throw;
                return;
            }

            logger.LogInformation("{FunctionName} completed at: {Time}", nameof(MeshHandshakeFunction), DateTime.UtcNow);
        }
    }
}
