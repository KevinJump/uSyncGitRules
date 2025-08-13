using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.GitEdition.Notifications;
internal class uSyncGitApplicationStartingHandler :
    INotificationAsyncHandler<UmbracoApplicationStartingNotification>,
    INotificationAsyncHandler<uSyncImportCompletedNotification>
{
    private readonly uSyncGitService _uSyncGitService;
    private readonly ILogger<uSyncGitApplicationStartingHandler> _logger;
    private readonly ISyncService _syncService;
    private readonly ISyncConfigService _syncConfigService;
    private readonly IConfiguration _configuration;

    public uSyncGitApplicationStartingHandler(
        uSyncGitService uSyncGitService,
        ILogger<uSyncGitApplicationStartingHandler> logger,
        ISyncService syncService,
        ISyncConfigService syncConfigService,
        IConfiguration configuration)
    {
        _uSyncGitService = uSyncGitService;
        _logger = logger;
        _syncService = syncService;
        _syncConfigService = syncConfigService;
        _configuration = configuration;
    }

    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken)
    {
        if (_uSyncGitService.HasRepo() is false)
        {
            _logger.LogWarning("[uSync] No git repository found. Skipping startup sync.");
            return;
        }


        if (_uSyncGitService.IsRepoDirty())
        {
            _logger.LogWarning("[uSync] git repository is dirty. no startup sync will be performed.");
        }
        else
        {
            _logger.LogInformation("[uSync] Git repository is clean.");

            if (await _uSyncGitService.SyncedSinceLastCommitAsync())
            {
                _logger.LogWarning("[uSync] The uSync commit is different to the git commit, performing a full import");
                await _syncService.StartupImportAsync(_syncConfigService.GetFolders(), false,
                    new SyncHandlerOptions
                    {
                        Group = _configuration.GetValue("uSync:GitSync", "all"),
                    });
            }
            else
            {
                _logger.LogInformation("[uSync] The last sync commit and the git commit are the same.");    
            }
        }
    }

    public async Task HandleAsync(uSyncImportCompletedNotification notification, CancellationToken cancellationToken)
    {
        // write the last commit to the meta file
        _logger.LogInformation("[uSync] Writing last commit to meta file.");
        await _uSyncGitService.WriteGitStatus();
    }
}
