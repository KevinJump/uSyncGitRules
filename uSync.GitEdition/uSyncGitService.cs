using LibGit2Sharp;

using Microsoft.Extensions.Logging;

using uSync.BackOffice.Services;

namespace uSync.GitEdition;
internal class uSyncGitService
{
    private const string metaFile = "~/uSync/git.meta";

    private readonly ILogger<uSyncGitService> _logger;
    private readonly ISyncFileService _fileService;

    public uSyncGitService(ILogger<uSyncGitService> logger, ISyncFileService fileService)
    {
        _logger = logger;
        _fileService = fileService;
    }

    private Repository GetRepo() 
        => new Repository(Repository.Discover("."));

    public async Task WriteLastSyncedCommitAsync()
    {
        var commit = GetLastCommit();
        await _fileService.SaveFileAsync(metaFile, commit);
    }

    public async Task<string> ReadLastSyncedCommitAsync()
    {
        if (_fileService.FileExists(metaFile) is false) return string.Empty;
        var commit = await _fileService.LoadContentAsync(metaFile);
        return commit ?? string.Empty;
    }


    public async Task<bool> SyncedSinceLastCommitAsync()
    {
        var LastSyncedCommit = await ReadLastSyncedCommitAsync();
        if (string.IsNullOrEmpty(LastSyncedCommit)) return false;

        var lastCommit = GetLastCommit();

        return lastCommit != LastSyncedCommit;
    }


    public string GetLastCommit()
    {
        using(var repo = GetRepo())
        {
            var commit = repo.Commits.FirstOrDefault();
            return commit?.Sha ?? string.Empty;
        }
    }

    public bool IsRepoDirty()
    {
        using (var repo = GetRepo())
        {
            RepositoryStatus status = repo.RetrieveStatus();
            return status.IsDirty;
        }
    }

    public void GetStatus()
    {
        using(var repo = GetRepo())
        {
            RepositoryStatus status = repo.RetrieveStatus();
            _logger.LogInformation("Status: {dirty}", status.IsDirty);
            foreach (var entry in status)
            {
                _logger.LogInformation("{State} - {FilePath}", entry.State, entry.FilePath);
            }
        }
    }

}
