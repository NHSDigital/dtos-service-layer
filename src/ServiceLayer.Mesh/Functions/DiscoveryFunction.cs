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
        private readonly QueueClient _queueClient;

        public DiscoveryFunction(ILogger<DiscoveryFunction> logger, IMeshInboxService meshInboxService, ServiceLayerDbContext serviceLayerDbContext, QueueClient queueClient)
        {
            _logger = logger;
            _meshInboxService = meshInboxService;
            _serviceLayerDbContext = serviceLayerDbContext;
            _queueClient = queueClient;
        }

        [Function("DiscoveryFunction")]
        public async Task Run([TimerTrigger("%DiscoveryTimerExpression%")] TimerInfo myTimer)
        {
            _logger.LogInformation($"DiscoveryFunction started at: {DateTime.Now}");

            var mailboxId = Environment.GetEnvironmentVariable("MailboxId")
                ?? throw new InvalidOperationException($"Environment variable 'MailboxId' is not set or is empty.");

            var response = await _meshInboxService.GetMessagesAsync(mailboxId);

            _queueClient.CreateIfNotExists();

            foreach (var messageId in response.Response.Messages)
            {
                using var transaction = await _serviceLayerDbContext.Database.BeginTransactionAsync();

                var existing = await _serviceLayerDbContext.MeshFiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.FileId == messageId);

                if (existing == null)
                {
                    _serviceLayerDbContext.MeshFiles.Add(new MeshFile
                    {
                        FileId = messageId,
                        FileType = MeshFileType.NbssAppointmentEvents,
                        MailboxId = mailboxId,
                        Status = MeshFileStatus.Discovered,
                        FirstSeenUtc = DateTime.UtcNow,
                        LastUpdatedUtc = DateTime.UtcNow
                    });

                    await _serviceLayerDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _queueClient.SendMessage(messageId);
                }
                else
                {
                    await transaction.RollbackAsync();
                }
            }
        }
    }
}
