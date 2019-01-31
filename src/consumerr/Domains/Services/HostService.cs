using Consumer.Configurations.Factories;
using Consumer.Domains.Models;
using Consumer.Domains.Models.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Consumer.Domains.Services
{
    public class HostService : BackgroundService
    {
        private string _tag;
        private Task _executingTask;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Messaging _messaging;
        private readonly IMessagingService<Message> _messagingService;
        private readonly IMessagingFactory _messagingFactory;
        private readonly IDatabaseFactory _databaseFactory;
        private readonly IOrchestratorService _orchestratorService;
        private readonly ILogger<HostService> _logger;
        
        public HostService(
            IMessagingFactory messagingFactory,
            IDatabaseFactory databaseFactory,
            IOrchestratorService orchestratorService,
            IMessagingService<Message> messagingService,
            IOptions<Messaging> messaging,
            ILogger<HostService> logger)
        {
            _messaging = messaging.Value ?? throw new ArgumentNullException(nameof(messaging));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _orchestratorService = orchestratorService ?? throw new ArgumentNullException(nameof(orchestratorService));
            _messagingService = messagingService ?? throw new ArgumentNullException(nameof(messagingService));
            _messagingFactory = messagingFactory ?? throw new ArgumentNullException(nameof(messagingFactory));
            _databaseFactory = databaseFactory ?? throw new ArgumentNullException(nameof(databaseFactory));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            _executingTask = ExecuteAsync(_cancellationTokenSource.Token);

            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
            {
                return;
            }

            _cancellationTokenSource.Cancel();

            await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));

            var channel = _messagingFactory.Configure();
            channel.BasicCancel(_tag);

            _messagingFactory.Disconnect();
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var channel = _messagingFactory.Configure();
            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.Received += _messagingService.Dequeue(async (raw, message) =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (message == null) 
                {
                    return;
                }

                using (_logger.BeginScope(Guid.NewGuid().ToString()))
                {
                    try
                    {
                        await _databaseFactory.OpenConnectionAsync();

                        await _databaseFactory.BeginTransactionAsync();

                        await _orchestratorService.OrchestrateAsync(message);

                        _databaseFactory.CommitTransaction();
                    }
                    catch (Exception ex) 
                    {
                        _databaseFactory.RollbackTransaction();

                        throw ex;
                    }
                    finally
                    {
                        _databaseFactory.CloseConnection();
                    }
                }
            });

            _tag = channel.BasicConsume(_messaging.Consuming.Queue, false, consumer);

            return Task.CompletedTask;
        }
    }
}
