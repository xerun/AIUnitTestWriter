using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AIUnitTestWriter
{
    public class Worker : BackgroundService
    {
        private readonly ProjectConfigModel _projectConfig;
        private readonly IGitMonitorService _gitIntegrationService;
        private readonly ILogger<Worker> _logger;

        public Worker(IGitMonitorService gitIntegrationService, ILogger<Worker> logger, ProjectConfigModel projectConfig)
        {
            _projectConfig = projectConfig ?? throw new ArgumentNullException(nameof(projectConfig));
            _gitIntegrationService = gitIntegrationService ?? throw new ArgumentNullException(nameof(gitIntegrationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            if (_projectConfig.IsGitRepository)
            {
                _logger.LogInformation("Git repository mode detected.");
                await _gitIntegrationService.MonitorAndTriggerAsync();
            }
            else
            {
                _logger.LogError("Git repository mode not detected. Exiting program.");
            }
        }
    }
}
