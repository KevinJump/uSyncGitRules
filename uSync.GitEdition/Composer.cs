using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice;
using uSync.GitEdition.Notifications;

namespace uSync.GitEdition;

[ComposeAfter(typeof(uSync.BackOffice.uSyncBackOfficeComposer))]
internal class Composer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<uSyncGitService>();

        // builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, uSyncGitApplicationStartingHandler>();
        builder.AddNotificationAsyncHandler<uSyncImportCompletedNotification, uSyncGitApplicationStartingHandler>();
    }
}

public static class  uSyncGitBootExtensions 
{
    public static IUmbracoBuilder AdduSyncGit(this IUmbracoBuilder builder)
    {
        builder.AddNotificationAsyncHandler<RuntimeUnattendedInstallNotification, uSyncGitUnattendedInstallHandler>();
        return builder;
    }
}
