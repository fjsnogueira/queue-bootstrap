using Consumer.Domain.Factories.Configurations;
using Consumer.Domain.Services;
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
                    configuration.AddJsonFile("appsettings.json", optional: true);
                    configuration.AddEnvironmentVariables();
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();

                    services.Configure<Messaging>(hostContext.Configuration);
                    services.Configure<Logging>(hostContext.Configuration);

                    services.AddHostedService<HostService>();
                })
                .Build();

            using (host)
            {
                await host.StartAsync();

                //var monitorLoop = host.Services.GetRequiredService<QueueService>();
                //monitorLoop.StartMonitorLoop();

                await host.WaitForShutdownAsync();
            }
        }
    }
}
