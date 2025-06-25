using LibGit2Sharp;

using Microsoft.Extensions.Logging;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using uSync.BackOffice.Services;

namespace uSync.GitEdition;

internal class uSyncGitConfig
{
    public string? Branch { get; set; }
    public string? Commit { get; set; }
}

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

    public async Task WriteGitStatus()
    {
        var config = new uSyncGitConfig
        {
            Branch = GetRepo().Head.FriendlyName,
            Commit = GetLastCommit()
        };
        await _fileService.SaveFileAsync(metaFile, JsonSerializer.Serialize(config));
    }

    public async Task<uSyncGitConfig> ReadGitStatus()
    {
        if (_fileService.FileExists(metaFile) is false) return new();
        var content = await _fileService.LoadContentAsync(metaFile);
        
        if (string.IsNullOrEmpty(content)) return new();

        return JsonSerializer.Deserialize<uSyncGitConfig>(content) 
            ?? new uSyncGitConfig();

    }


    public async Task<bool> SyncedSinceLastCommitAsync()
    {
        var config = await ReadGitStatus();
        if (string.IsNullOrEmpty(config.Commit)) return false;

        var lastCommit = GetLastCommit();

        return lastCommit != config.Commit;
    }

    private string GetBranchName()
    {
        using (var repo = GetRepo())
        {
            return repo.Head.FriendlyName;
        }
    }


    public async Task<bool> HasBranchChanged()
    {
        var currentBranch = GetBranchName();
        var config = await ReadGitStatus();

        if (config.Branch is null) return false;
        return config.Branch != currentBranch;
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
