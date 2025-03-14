using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Services;
using AIUnitTestWriter.Services.Git;
using AIUnitTestWriter.SettingOptions;
using AIUnitTestWriter.Wrappers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO.Abstractions;

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
                    services.AddSingleton(context.Configuration);

                    services.AddHttpClient();

                    services.Configure<ProjectSettings>(context.Configuration.GetSection("Project"));
                    services.Configure<AISettings>(context.Configuration.GetSection("AI"));
                    services.Configure<GitSettings>(context.Configuration.GetSection("Git"));
                    services.Configure<SkippedFilesSettings>(context.Configuration.GetSection("SkippedFiles"));

                    // Register your services.
                    services.AddSingleton<IProjectInitializer, ProjectInitializerService>();
                    services.AddSingleton(provider =>
                    {
                        var initializer = provider.GetRequiredService<IProjectInitializer>();
                        return initializer.Initialize();
                    });                    
                    services.AddSingleton<IHttpRequestMessageFactory, HttpRequestMessageFactory>();
                    services.AddSingleton<IGitProcessFactory, GitProcessFactory>();
                    services.AddSingleton<IFileSystem, FileSystem>();
                    services.AddSingleton<IFileWatcherWrapper, FileWatcherWrapper>();
                    services.AddSingleton<IDelayService, DelayService>();
                    services.AddSingleton<ISkippedFilesManager, SkippedFilesManager>();
                    services.AddSingleton<IModeRunner, ModeRunnerService>();
                    services.AddSingleton<IAIApiService, AIApiService>();
                    services.AddSingleton<ICodeMonitor, CodeMonitor>();
                    services.AddTransient<ITestUpdaterService, TestUpdaterService>();
                    services.AddSingleton<ICodeAnalyzer, CodeAnalyzer>();
                    services.AddSingleton<IGitHubClientWrapper, GitHubClientWrapper>();
                    services.AddSingleton<IGitProcessService, GitProcessService>();
                    services.AddSingleton<IGitMonitorService, GitMonitorService>();
                    services.AddSingleton<IConsoleService, ConsoleService>();

                    // Register the Application runner.
                    services.AddSingleton<AppStarter>();
                })
                .Build();

            // Run the application.
            await host.Services.GetRequiredService<AppStarter>().RunAsync();
        }
    }
}
