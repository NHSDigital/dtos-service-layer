using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Contracts.Services;
using ServiceLayer.Mesh.Data;

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

            var response = await _meshInboxService.GetMessagesAsync(Environment.GetEnvironmentVariable("MailboxId"));

            if (response.Response.Messages.Count() > 500)
            {
                // TODO: Get next page
                // dotnet-mesh-client needs to be updated to support pagination for when inbox containers more than 500 messages
            }

            foreach (var message in response.Response.Messages)
            {
                // Check if message has been seen before
                var doesFileIdExist = await _serviceLayerDbContext.MeshFiles.AnyAsync(m => m.FileId == message.ToString());


                // If no then insert into db
                // Then enqueue message

                // If no then do nothing
            }

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
