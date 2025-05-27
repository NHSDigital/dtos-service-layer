using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Mesh.Configuration;
using ServiceLayer.Mesh.Extensions;

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
            logger.LogInformation("{FunctionName} started.", nameof(MeshHandshakeFunction));

            try
            {
                var response = await meshOperationService.MeshHandshakeAsync(configuration.NbssMeshMailboxId);

                if (response.IsSuccessful)
                {
                    logger.LogInformation(
                        "Mesh handshake completed successfully for mailbox {ConfigurationNbssMeshMailboxId}.", configuration.NbssMeshMailboxId);
                }
                else
                {
                    logger.LogWarning("Mesh handshake failed for mailbox {ConfigurationNbssMeshMailboxId}: [ {ToFormattedString} ]", configuration.NbssMeshMailboxId, response.Error.ToFormattedString());
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "An error occurred during mesh handshake for mailbox {MailboxId}.", configuration.NbssMeshMailboxId);
            }
        }
    }
}
