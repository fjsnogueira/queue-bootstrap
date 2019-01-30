
using Consumer.Configurations.Factories;
using Consumer.Domains.Models;
using Consumer.Domains.Models.Options;
using Consumer.Domains.Services;
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
                    configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
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
                    services.Configure<Database>(hostContext.Configuration.GetSection("Database"));

                    services.AddScoped<IDatabaseFactory, DatabaseFactory>();
                    services.AddScoped<IMessagingFactory, MessagingFactory>();

                    services.AddTransient<ISqlService, SqlService>();
                    services.AddTransient<IOrderService, OrderService>();
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
