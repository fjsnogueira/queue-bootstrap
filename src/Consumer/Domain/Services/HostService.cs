using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Consumer.Domain.Services
{
    public class HostService : BackgroundService
    {
        private Task _executingTask;
        private CancellationTokenSource _cts;

        public HostService(/*ILogger<MsmqService> logger, IMsmqConnection connection, IMsmqProcessor processor*/)
        {
            //Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            //Connection = connection ?? throw new ArgumentNullException(nameof(connection));
            //Processor = processor ?? throw new ArgumentNullException(nameof(processor));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            // Create a linked token so we can trigger cancellation outside of this token's cancellation
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Store the task we're executing
            _executingTask = ExecuteAsync(_cts.Token);

            // If the task is completed then return it
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            //abrir conexoes com banco e messaging

            // Otherwise it's running
            return Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            // Stop called without start
            if (_executingTask == null)
            {
                return;
            }

            // Signal cancellation to the executing method
            _cts.Cancel();

            // Wait until the task completes or the stop token triggers
            await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));

            //fechar conexoes com banco e messaging

            // Throw if cancellation triggered
            cancellationToken.ThrowIfCancellationRequested();
        }

        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                consumidorService.ProcessarMensagensFilaAsync(configRabbitMQ, Listen, async (excecao, mensagem) =>
                {
                    if (excecao == null)
                    {
                        var repo = serviceProvider.GetService<ITransacaoRepository>();
                        var teste = await repo.GetById(mensagem.IdTransacao);

                        // await orquestrador.RealizarFluxoDeCadastros(mensagem.IdCredenciamento);
                    }
                });
            }
            
            return Task.CompletedTask;
        }
    }
}
