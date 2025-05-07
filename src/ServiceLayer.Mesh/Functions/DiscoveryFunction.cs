using Azure.Identity;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Mesh.Data;
using ServiceLayer.Mesh.Models;

namespace ServiceLayer.Mesh.Functions
{
    public class DiscoveryFunction
    {
        private readonly ILogger _logger;
        private readonly IMeshInboxService _meshInboxService;
        private readonly ServiceLayerDbContext _serviceLayerDbContext;

        public DiscoveryFunction(ILoggerFactory loggerFactory, IMeshInboxService meshInboxService, ServiceLayerDbContext serviceLayerDbContext)
        {
            _logger = loggerFactory.CreateLogger<DiscoveryFunction>();
            _meshInboxService = meshInboxService;
            _serviceLayerDbContext = serviceLayerDbContext;
        }

        [Function("DiscoveryFunction")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var mailboxId = Environment.GetEnvironmentVariable("MailboxId")
                ?? throw new InvalidOperationException($"Environment variable 'MailboxId' is not set or is empty.");

            var response = await _meshInboxService.GetMessagesAsync(mailboxId);

            if (response.Response.Messages.Count() > 500)
            {
                // TODO: Get next page
                // dotnet-mesh-client needs to be updated to support pagination for when inbox containers more than 500 messages
            }

            foreach (var messageId in response.Response.Messages)
            {
                // Check if message has been seen before
                var doesFileIdExist = await _serviceLayerDbContext.MeshFiles.AnyAsync(m => m.FileId == messageId.ToString());

                if (!doesFileIdExist)
                {
                    var meshFile = new MeshFile()
                    {
                        FileId = messageId,
                        FileType = "",
                        MailboxId = mailboxId,
                        Status = "Discovered"
                    };

                    await _serviceLayerDbContext.MeshFiles.AddAsync(meshFile);
                    await _serviceLayerDbContext.SaveChangesAsync();

                    QueueClient queueClient;

                    if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                    {
                        queueClient = new QueueClient("UseDevelopmentStorage=true", "my-local-queue");
                    }
                    else
                    {
                        var credential = new ManagedIdentityCredential();
                        queueClient = new QueueClient(new Uri(Environment.GetEnvironmentVariable("QueueUrl")), credential);
                    }

                    queueClient.CreateIfNotExists();
                    queueClient.SendMessage(messageId);
                }
            }

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
