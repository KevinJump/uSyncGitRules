using LibGit2Sharp;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice;
using uSync.BackOffice.Configuration;
using uSync.BackOffice.SyncHandlers.Models;

namespace uSync.GitEdition;
internal class uSyncGitNotificationHandler :
    INotificationAsyncHandler<UmbracoApplicationStartingNotification>,
    INotificationAsyncHandler<uSyncImportCompletedNotification>
{
    private readonly uSyncGitService _uSyncGitService;
    private readonly ILogger<uSyncGitNotificationHandler> _logger;
    private readonly ISyncService _syncService;
    private readonly ISyncConfigService _syncConfigService;

    public uSyncGitNotificationHandler(
        uSyncGitService uSyncGitService,
        ILogger<uSyncGitNotificationHandler> logger,
        ISyncService syncService,
        ISyncConfigService syncConfigService)
    {
        _uSyncGitService = uSyncGitService;
        _logger = logger;
        _syncService = syncService;
        _syncConfigService = syncConfigService;
    }

    public async Task HandleAsync(UmbracoApplicationStartingNotification notification, CancellationToken cancellationToken)
    {
        // TODO: Check last committed sync data. 
        // var commit = _uSyncGitService.GetLastCommit();
        // _logger.LogInformation("Last commit - > {commit}", commit);


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
                        Group = "All"
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
        await _uSyncGitService.WriteLastSyncedCommitAsync();
    }
}
