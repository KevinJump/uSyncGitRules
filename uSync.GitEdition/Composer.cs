using Microsoft.Extensions.DependencyInjection;

using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;

using uSync.BackOffice;

namespace uSync.GitEdition;

[ComposeAfter(typeof(uSync.BackOffice.uSyncBackOfficeComposer))]
internal class Composer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<uSyncGitService>();

        builder.AddNotificationAsyncHandler<UmbracoApplicationStartingNotification, uSyncGitNotificationHandler>();
        builder.AddNotificationAsyncHandler<uSyncImportCompletedNotification, uSyncGitNotificationHandler>();
    }
}
