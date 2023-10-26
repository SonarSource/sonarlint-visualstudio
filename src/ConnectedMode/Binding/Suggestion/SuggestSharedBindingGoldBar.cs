using System;
using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;

namespace SonarLint.VisualStudio.ConnectedMode.Binding.Suggestion
{
    internal interface ISuggestSharedBindingGoldBar
    {
        void Show(Action onConnectHandler);
    }

    [Export(typeof(ISuggestSharedBindingGoldBar))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal class SuggestSharedBindingGoldBar : ISuggestSharedBindingGoldBar
    {
        private readonly INotificationService notificationService;
        private readonly IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction;
        private readonly ISolutionInfoProvider solutionInfoProvider;
        private readonly IBrowserService browserService;

        internal /* for testing */ const string IdTemplate = "shared.binding.suggestion.for.{0}";

        [ImportingConstructor]
        public SuggestSharedBindingGoldBar(INotificationService notificationService, 
            IDoNotShowAgainNotificationAction doNotShowAgainNotificationAction,
            ISolutionInfoProvider solutionInfoProvider,
            IBrowserService browserService)
        {
            this.notificationService = notificationService;
            this.doNotShowAgainNotificationAction = doNotShowAgainNotificationAction;
            this.solutionInfoProvider = solutionInfoProvider;
            this.browserService = browserService;
        }

        public void Show(Action onConnectHandler)
        {
            var notification = new Notification(
                id: string.Format(IdTemplate, solutionInfoProvider.GetSolutionName()),
                message: BindingStrings.SharedBindingSuggestionMainText,
                actions: new INotificationAction[]
                {
                    new NotificationAction(BindingStrings.SharedBindingSuggestionConnectOptionText, _ => onConnectHandler(), true),
                    new NotificationAction(BindingStrings.SharedBindingSuggestionInfoOptionText, _ => OnLearnMore(), false),
                    doNotShowAgainNotificationAction
                });

            notificationService.ShowNotification(notification);
        }

        private void OnLearnMore()
        {
            browserService.Navigate(DocumentationLinks.SharedBinding);
        }
    }
}
