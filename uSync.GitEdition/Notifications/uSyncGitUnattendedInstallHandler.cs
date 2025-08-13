using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Extensions;
using Umbraco.Cms.Core.Install.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations.Install;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Extensions;

namespace uSync.GitEdition.Notifications;

/// <summary>
/// Handles unattended installation notifications for uSync Git Edition, providing Git-aware database configuration and setup.
/// </summary>
/// <remarks>
/// This handler manages database configuration based on Git branch information during unattended Umbraco installations.
/// It can automatically configure branch-specific databases and handle database cleanup on branch changes.
/// </remarks>
internal class uSyncGitUnattendedInstallHandler : INotificationAsyncHandler<RuntimeUnattendedInstallNotification>
{
    private IHostEnvironment _hostEnvironment;

    private readonly ILogger<uSyncGitUnattendedInstallHandler> _logger;
    private readonly IUmbracoDatabaseFactory _databaseFactory;
    private readonly uSyncGitService _uSyncGitService;
    private readonly IConfigManipulator _configManipulator;
    private readonly IEnumerable<IDatabaseProviderMetadata> _databaseProviderMetadata;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="uSyncGitUnattendedInstallHandler"/> class.
    /// </summary>
    public uSyncGitUnattendedInstallHandler(
        ILogger<uSyncGitUnattendedInstallHandler> logger,
        IUmbracoDatabaseFactory databaseFactory,
        IRuntimeState runtimeState,
        IHostEnvironment hostEnvironment,
        uSyncGitService uSyncGitService,
        IEnumerable<IDatabaseProviderMetadata> databaseProviderMetadata,
        DatabaseBuilder databaseBuilder,
        IConfiguration configuration,
        IConfigManipulator configManipulator)
    {
        _logger = logger;
        _databaseFactory = databaseFactory;
        _hostEnvironment = hostEnvironment;
        _uSyncGitService = uSyncGitService;
        _databaseProviderMetadata = databaseProviderMetadata;
        _configuration = configuration;
        _configManipulator = configManipulator;
    }

    /// <summary>
    /// Handles the runtime unattended install notification asynchronously.
    /// </summary>
    /// <param name="notification">The runtime unattended install notification.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method orchestrates the Git-aware installation process by:
    /// 1. Checking if the database is configured - exits early if not configured
    /// 2. Optionally deleting database files on branch changes if configured
    /// 3. Setting up branch-specific database naming if enabled
    /// The method respects configuration settings for Git:DB:DeleteOnBranchChange and Git:DB:NameByBranch.
    /// </remarks>
    public async Task HandleAsync(RuntimeUnattendedInstallNotification notification, CancellationToken cancellationToken)
    {
        // Handle the unattended install logic here
        // For example, you might want to initialize Git repositories or perform other setup tasks
        if (_uSyncGitService.HasRepo() is false)
        {
            _logger.LogWarning("No git repository found, exiting uSyncGit handler");
            return;
        }

        if (_databaseFactory.Configured == false)
        {
            // going to assume this site has never been installed, so we just exit here.
            _logger.LogInformation("Database is not configured, exiting uSyncGit handler");
            return;
        }

        if (_configuration.GetValue("Git:Db:DeleteOnBranchChange", false))
        {
            _logger.LogInformation("Git:Db:DeleteOnBranchChange is enabled, checking for branch rename.");
            await DeleteDBFileOnBranchRename();
        }

        if (_configuration.GetValue("Git:Db:NameByBranch", true) is false)
        {
            _logger.LogInformation("Git:Db:NameByBranch is disabled, exiting uSyncGit handler");
            return;
        }

        await SetConnectionNameByBranch(_configuration.GetValue("Git:DB:Template", "Umbraco.{branch}-{ticks}"));
    }

    /// <summary>
    /// Sets the database connection name based on the current Git branch using a configurable template.
    /// </summary>
    /// <param name="template">The template string for generating the database name, supporting {branch} and {ticks} placeholders.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method creates branch-specific database configurations by:
    /// 1. Replacing template placeholders with current branch name and timestamp ticks
    /// 2. Generating a new database model with the computed name
    /// 3. Creating connection strings using available database providers
    /// 4. Updating the configuration if the connection string differs from the current one
    /// 5. Configuring the database factory and creating the database
    /// The method ensures each Git branch can have its own isolated database instance.
    /// </remarks>
    private async Task SetConnectionNameByBranch(string template)
    { 
        var replacements = new Dictionary<string, string>
        {
            { "{branch}", _uSyncGitService.GetCurrentBranchName() },
            { "{ticks}", DateTime.Now.Ticks.ToString() },
            { "{env}", _hostEnvironment.EnvironmentName }
        };
        

        var databaseModel = new DatabaseModel
        {
            DatabaseName = template.ReplaceMany(replacements)
        };

        var providerMeta = _databaseProviderMetadata.GetAvailable(true).FirstOrDefault();
        if (providerMeta == null)
        {
            _logger.LogError("No database provider metadata found, exiting uSyncGit handler");
            return;
        }

        var connectionString = providerMeta.GenerateConnectionString(databaseModel);
        var providerName = providerMeta.ProviderName;

        if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(providerName))
        {
            _logger.LogError("Connection string or provider name is empty, exiting uSyncGit handler");
            return;
        }

        var currentConnectionString = _configuration.GetConnectionString(Constants.System.UmbracoConnectionName);
        if (currentConnectionString?.Equals(connectionString, StringComparison.OrdinalIgnoreCase) is true)
        {
            _logger.LogInformation("Connection string is already set to the current branch, no changes needed.");
            return;
        }

        await _configManipulator.SaveConnectionStringAsync(connectionString, providerName);

        var expandedConnectionString = ExpandConnectionString(connectionString);

        _databaseFactory.Configure(new ConnectionStrings
        {
            ConnectionString = ExpandConnectionString(connectionString),
            ProviderName = providerName
        });

        _databaseFactory.CreateDatabase();
    }

    /// <summary>
    /// Expands placeholders in the connection string, particularly the data directory placeholder.
    /// </summary>
    /// <param name="connectionString">The connection string that may contain placeholders.</param>
    /// <returns>The expanded connection string with placeholders replaced by actual values.</returns>
    /// <remarks>
    /// This method specifically handles the data directory placeholder replacement by:
    /// 1. Retrieving the current application domain's data directory
    /// 2. Replacing the Constants.System.DataDirectoryPlaceholder with the actual directory path
    /// 3. Returning the original string if no data directory is configured
    /// This ensures that relative paths in connection strings are properly resolved to absolute paths.
    /// </remarks>
    private static string ExpandConnectionString(string connectionString)
    {
        // Replace data directory
        string? dataDirectory = AppDomain.CurrentDomain.GetData(Constants.System.DataDirectoryName)?.ToString();
        if (!string.IsNullOrEmpty(dataDirectory))
        {
            return connectionString.Replace(Constants.System.DataDirectoryPlaceholder, dataDirectory);
        }

        return connectionString;
    }

    /// <summary>
    /// Deletes database files when a Git branch change is detected and conditions are met.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method implements branch-change database cleanup by:
    /// 1. Checking if the Umbraco data directory exists
    /// 2. Detecting if the Git branch has changed since last sync
    /// 3. Verifying if uSync has been synced since the last Git commit
    /// 4. Deleting SQLite database files (Umbraco.sqlite.*) in the data folder if conditions are met
    /// The cleanup only occurs when there's a branch change and uSync is out of sync with Git,
    /// ensuring database state consistency across different branches.
    /// </remarks>
    private async Task DeleteDBFileOnBranchRename() { 
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
            return;
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
