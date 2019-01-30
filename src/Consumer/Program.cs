
using Consumer.Configurations.Factories;
using Consumer.Domains.Models;
using Consumer.Domains.Models.Options;
using Consumer.Domains.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Consumer
{
    public class Program
    {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{ Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production" }.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        public static IHost BuildHost(string[] args) => new HostBuilder()
            .ConfigureAppConfiguration((hostContext, configuration) =>
            {
                configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                configuration.AddEnvironmentVariables();
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
            .UseSerilog()
            .Build();

        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            try
            {
                var host = BuildHost(args);

                using (host)
                {
                    await host.StartAsync();

                    await host.WaitForShutdownAsync();
                }
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
