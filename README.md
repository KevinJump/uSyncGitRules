# uSync Git Edition

🚨 **EXPERIMENTAL CODE - USE WITH CAUTION** 🚨

This project is experimental and should not be used in production environments without thorough testing.

## Overview

uSync Git Edition is an experimental extension for Umbraco CMS that provides Git-aware database management capabilities. The primary goal of this project is to automatically manage Umbraco databases when switching between Git branches, enabling developers to maintain branch-specific database states during development.

## Key Features

### 🔄 Branch-Specific Database Management
- Automatically creates and manages separate databases for different Git branches
- Uses configurable naming templates (e.g., `Umbraco.{branch}.{ticks}`)
- Prevents database conflicts when switching between feature branches

### 🧹 Automatic Database Cleanup
- Optionally deletes database files when changing branches
- Configurable cleanup behavior based on uSync synchronization status
- Helps maintain clean development environments

### ⚡ Unattended Installation Support
- Handles database configuration during Umbraco's unattended installation process
- Integrates with Umbraco's runtime installation notifications
- Automatically sets up Git-aware database configurations

## Project Structure

```
uSyncGitRules/
├── uSync.GitEdition/          # Core Git Edition library
│   ├── Notifications/         # Event handlers for Umbraco notifications
│   ├── uSyncGitService.cs    # Git operations and metadata management
│   └── Composer.cs           # Dependency injection setup
└── uSync.Site/               # Example Umbraco site implementation
    ├── appsettings.json      # Configuration examples
    └── Program.cs            # Site startup configuration
```

## Configuration

Add the following configuration to your `appsettings.json`:

```json
{
  "Git": {
    "Db": {
      "DeleteOnBranchChange": false,
      "NameByBranch": true,
      "Template": "Umbraco.{branch}.{ticks}"
    }
  },
  "uSync": {
    "Settings": {
      "ImportOnFirstBoot": true
    }
  }
}
```

### Configuration Options

- **`DeleteOnBranchChange`**: When `true`, deletes existing database files when switching branches
- **`NameByBranch`**: When `true`, enables branch-specific database naming
- **`Template`**: Database naming template supporting `{branch}`,`{ticks}` and `{env}` placeholders

## Getting Started

1. **Add the Extension**: Include the uSync.GitEdition project reference in your Umbraco site
2. (optionally) **Update Configuration**: Add Git database settings to your `appsettings.json`
3. **Initialize**: The extension will automatically handle database management during application startup

## How It Works

1. **Branch Detection**: Uses LibGit2Sharp to detect the current Git branch
2. **Database Naming**: Generates branch-specific database names using configurable templates
3. **Synchronization Tracking**: Maintains metadata about Git commits and uSync synchronization status
4. **Automatic Management**: Handles database creation, configuration, and cleanup based on Git state changes

## Requirements

- **.NET 9.0**
- **Umbraco CMS 15.4.1+**
- **uSync 15.1.6+**
- **Git repository** (project must be in a Git repository)

## Dependencies

- `Umbraco.Cms` (15.4.1)
- `uSync.BackOffice` (15.1.6)
- `LibGit2Sharp` (0.31.0)

## ⚠️ Important Warnings

### Experimental Status
- This code is **experimental** and may contain bugs
- **Do not use in production environments**
- Always backup your databases before testing
- Test thoroughly in isolated development environments

### Data Loss Risk
- The database cleanup feature can **permanently delete database files**
- Ensure you have proper backups before enabling `DeleteOnBranchChange`
- Test the cleanup behavior in non-critical environments first

### Git Repository Requirement
- The project **must be run within a Git repository**
- Git operations may fail if the repository is in an invalid state
- Ensure your Git repository is properly initialized and configured

## Development Workflow

1. **Feature Branch**: Create a new Git branch for your feature
2. **Automatic Setup**: The extension automatically creates a branch-specific database
3. **Development**: Work on your feature with isolated database state
4. **Branch Switching**: When switching branches, the extension manages database transitions
5. **Cleanup**: Optionally clean up databases when branches are merged or deleted

## Troubleshooting

### Logging
The extension provides detailed logging. Check your Umbraco logs for:
- `uSyncGitUnattendedInstallHandler` messages
- `uSyncGitApplicationStartingHandler` messages
- Git operation status and errors

## Contributing

This is an experimental project. If you encounter issues or have suggestions:

1. Test thoroughly in isolated environments
2. Document any issues with detailed reproduction steps
3. Consider the experimental nature when reporting bugs

## License

This project follows the same licensing as the main uSync project. Please refer to the uSync license for details.

---

**Remember: This is experimental software. Always backup your data and test in non-production environments first!**