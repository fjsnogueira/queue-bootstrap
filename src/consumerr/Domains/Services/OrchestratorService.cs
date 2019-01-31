using System;
using System.Threading;
using System.Threading.Tasks;
using Consumer.Domains.Models;
using Microsoft.Extensions.Logging;

namespace Consumer.Domains.Services
{
    public interface IOrchestratorService
    {
        Task OrchestrateAsync(Message message);
    }

    public class OrchestratorService : IOrchestratorService
    {
        private readonly ILogger<OrchestratorService> _logger;

        public OrchestratorService(
            ILogger<OrchestratorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task OrchestrateAsync(Message message)
        {
            this._logger.LogInformation($"START | Message: { message.Id }");

            await Task.Delay(10000);

            this._logger.LogInformation($"END | Message: { message.Id }");
        }
    }
}
