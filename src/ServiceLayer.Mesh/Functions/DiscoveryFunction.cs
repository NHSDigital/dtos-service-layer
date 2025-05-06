using System;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using NHS.MESH.Client.Services;
using NHS.MESH.Client.Models;
using NHS.MESH.Client.Contracts.Services;
using System.Threading.Tasks;

namespace ServiceLayer.Mesh.Functions
{
    public class DiscoveryFunction
    {
        private readonly ILogger _logger;

        private readonly IMeshInboxService _meshInboxService;

        public DiscoveryFunction(ILoggerFactory loggerFactory, IMeshInboxService meshInboxService)
        {
            _logger = loggerFactory.CreateLogger<DiscoveryFunction>();
            _meshInboxService = meshInboxService;
        }

        [Function("DiscoveryFunction")]
        public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var response = await _meshInboxService.GetMessagesAsync(Environment.GetEnvironmentVariable("MailboxId"));

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
