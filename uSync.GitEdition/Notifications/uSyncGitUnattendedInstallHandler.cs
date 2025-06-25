using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Extensions;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;

namespace uSync.GitEdition.Notifications;
internal class uSyncGitUnattendedInstallHandler : INotificationAsyncHandler<RuntimeUnattendedInstallNotification>
{
    private IHostEnvironment _hostEnvironment;

    private readonly ILogger<uSyncGitUnattendedInstallHandler> _logger;
    private readonly IUmbracoDatabaseFactory _databaseFactory;
    private readonly IRuntimeState _runtimeState;
    private readonly uSyncGitService _uSyncGitService;

    public uSyncGitUnattendedInstallHandler(
        ILogger<uSyncGitUnattendedInstallHandler> logger,
        IUmbracoDatabaseFactory databaseFactory,
        IRuntimeState runtimeState,
        IHostEnvironment hostEnvironment,
        uSyncGitService uSyncGitService)
    {
        _logger = logger;
        _databaseFactory = databaseFactory;
        _runtimeState = runtimeState;
        _hostEnvironment = hostEnvironment;
        _uSyncGitService = uSyncGitService;
    }

    public async Task HandleAsync(RuntimeUnattendedInstallNotification notification, CancellationToken cancellationToken)
    {
        // Handle the unattended install logic here
        // For example, you might want to initialize Git repositories or perform other setup tasks

        if (_databaseFactory.Configured == false)
        {
            // going to assume this site has never been installed, so we just exit here.
            _logger.LogInformation("Database is not configured, exiting uSyncGit handler");
            return;
        }

        // we can't ask for the database here, as we might want to delete it! 
        var folder = _hostEnvironment.MapPathContentRoot("~/umbraco/Data/");

        if (Directory.Exists(folder) is false)
        {
            _logger.LogInformation("Data folder does not exist, exiting uSyncGit handler");
            return;
        }

        if (await _uSyncGitService.HasBranchChanged())
        {
            _logger.LogWarning("Branch has changed since the last sync -> checking....");
            if (await _uSyncGitService.SyncedSinceLastCommitAsync())
            {
                _logger.LogWarning("uSync has not been synced since the last git commit, performing a full import.");
            }
            else
            {
                _logger.LogInformation("uSync is in sync with the git repository, no import needed.");
                return;
            }
        }
        else
        {
            _logger.LogInformation("uSync branch has not changed, no import needed.");
        }

        foreach(var file in Directory.GetFiles(folder, "Umbraco.sqlite.*"))
        {
            _logger.LogInformation("Found database file: {File}", file);
            File.Delete(file);
            _logger.LogInformation("Deleted database file: {File}", file);

        }

        _logger.LogInformation("Handling unattended install notification for uSyncGit.");

        return;
    }
}
