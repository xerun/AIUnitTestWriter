using AIUnitTestWriter.Services;
using AIUnitTestWriter.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AIUnitTestWriter
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Build a host with configuration and DI.
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var env = context.HostingEnvironment.EnvironmentName;
                    config.SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json", false, true)
                          .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                          .AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    // Register configuration
                    services.AddSingleton<IConfiguration>(context.Configuration);

                    // Register your services.
                    services.AddSingleton<IProjectInitializer, ProjectInitializerService>();
                    services.AddSingleton<IModeRunner, ModeRunnerService>();
                    services.AddSingleton<IAIApiService, AIApiService>();
                    services.AddSingleton<ICodeMonitor, CodeMonitor>();
                    services.AddTransient<ITestUpdater, TestUpdater>();
                    services.AddSingleton<ICodeAnalyzer, CodeAnalyzer>();
                    services.AddSingleton<IGitIntegrationService, GitIntegrationService>();

                    // Register the Application runner.
                    services.AddSingleton<AppStarter>();
                })
                .Build();

            // Run the application.
            await host.Services.GetRequiredService<AppStarter>().RunAsync();
        }
    }
}
