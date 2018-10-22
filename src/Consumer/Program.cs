using Consumer.Domain.Factories.Configurations;
using Consumer.Domain.Models;
using Consumer.Domain.Services;
using Consumidor.Infraestrutura.RabbitMQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Consumer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder() 
                .ConfigureAppConfiguration((hostContext, configuration) =>
                {
                    configuration.AddJsonFile("appsettings.json", optional: false);
                    configuration.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();

                    services.Configure<Messaging>(hostContext.Configuration.GetSection("Messaging"));
                    services.Configure<Logging>(hostContext.Configuration.GetSection("Logging"));
                    services.Configure<Database>(hostContext.Configuration.GetSection("Database"));

                    services.AddSingleton<IMessagingFactory, MessagingFactory>();
                    // services.AddSingleton<IDatabaseFactory, DatabaseFactory>();
                    // services.AddSingleton<ILoggingFactory, LoggingFactory>();

                    services.AddTransient<IOrchestratorService, OrchestratorService>();
                    services.AddTransient<IMessagingService<Message>, MessagingService<Message>>();

                    services.AddHostedService<HostService>();
                })
                .Build();

            using (host)
            {
                await host.StartAsync();

                await host.WaitForShutdownAsync();
            }
        }
    }
}
