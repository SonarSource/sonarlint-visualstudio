/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2024 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System.ComponentModel.Composition;
using SonarLint.VisualStudio.Core.InfoBar;

namespace SonarLint.VisualStudio.Core.Notifications;

/// <summary>
/// This service can be used to display any type of user visible notification. Each instance of this service
/// will only show one notification at a time i.e. showing a second notification will remove the first one.
/// </summary>
/// <remarks>The caller can assume that the component follows VS threading rules
/// i.e. the implementing class is responsible for switching to the UI thread if necessary.
/// The caller doesn't need to worry about it.
/// </remarks>
public interface INotificationService : IDisposable
{
    void ShowNotification(INotification notification);
        
    void ShowNotification(INotification notification, Guid toolWindowId);

    void CloseNotification();
}

[Export(typeof(INotificationService))]
[PartCreationPolicy(CreationPolicy.NonShared)]
internal sealed class NotificationService : INotificationService
{
    private static readonly Guid MainWindowId = Guid.Empty;
        
    private readonly IInfoBarManager infoBarManager;
    private readonly IDisabledNotificationsStorage notificationsStorage;
    private readonly IThreadHandling threadHandling;
    private readonly ILogger logger;

    private readonly HashSet<string> oncePerSessionNotifications = [];

    private Tuple<IInfoBar, INotification> activeNotification;

    [ImportingConstructor]
    public NotificationService(IInfoBarManager infoBarManager,
        IDisabledNotificationsStorage notificationsStorage,
        IThreadHandling threadHandling,
        ILogger logger)
    {
        this.infoBarManager = infoBarManager;
        this.notificationsStorage = notificationsStorage;
        this.threadHandling = threadHandling;
        this.logger = logger;
    }

    public void ShowNotification(INotification notification)
    {
        ShowNotification(notification, MainWindowId);
    }

    public void ShowNotification(INotification notification, Guid toolWindowId)
    {
        if (notification == null)
        {
            throw new ArgumentNullException(nameof(notification));
        }

        if (notificationsStorage.IsNotificationDisabled(notification.Id))
        {
            logger.LogVerbose($"[NotificationService] notification '{notification.Id}' will not be shown: notification is blocked.");
            return;
        }

        if (oncePerSessionNotifications.Contains(notification.Id))
        {
            logger.LogVerbose($"[NotificationService] notification '{notification.Id}' will not be shown: notification has already been displayed.");
            return;
        }

        threadHandling.RunOnUIThreadAsync(() =>
        {
            CloseNotification();
            ShowInfoBar(notification, toolWindowId);
        });
    }

    public void CloseNotification()
    {
        if (activeNotification == null)
        {
            return;
        }
            
        threadHandling.RunOnUIThreadAsync(() =>
        {
            try
            {
                infoBarManager.CloseInfoBar(activeNotification.Item1);
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(CoreStrings.Notifications_FailedToRemove, ex);
            }
        });
    }

    private void CurrentInfoBar_ButtonClick(object sender, InfoBarButtonClickedEventArgs e)
    {
        try
        {
            var notification = activeNotification.Item2;
            var matchingAction = notification.Actions.FirstOrDefault(x => x.CommandText == e.ClickedButtonText);

            matchingAction?.Action(notification);

            if (matchingAction?.ShouldDismissAfterAction == true)
            {
                CloseNotification();
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(CoreStrings.Notifications_FailedToExecuteAction, ex);
        }
    }

    private void CurrentInfoBar_Closed(object sender, EventArgs e)
    {
        if (activeNotification == null)
        {
            return;
        }

        activeNotification.Item1.ButtonClick -= CurrentInfoBar_ButtonClick;
        activeNotification.Item1.Closed -= CurrentInfoBar_Closed;
        activeNotification = null;
    }

    private void ShowInfoBar(INotification notification, Guid toolWindowId)
    {
        try
        {
            var infoBar = AttachInfoBar(notification, toolWindowId);
            activeNotification = new Tuple<IInfoBar, INotification>(infoBar, notification);
            activeNotification.Item1.ButtonClick += CurrentInfoBar_ButtonClick;
            activeNotification.Item1.Closed += CurrentInfoBar_Closed;
            if (notification.ShowOncePerSession)
            {
                oncePerSessionNotifications.Add(notification.Id);
            }
        }
        catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
        {
            logger.WriteLine(CoreStrings.Notifications_FailedToDisplay, notification.Id, ex);
        }
    }

    private IInfoBar AttachInfoBar(INotification notification, Guid toolWindowId)
    {
        var buttonTexts = notification.Actions.Select(x => x.CommandText).ToArray();

        if (toolWindowId == MainWindowId)
        {
            return infoBarManager.AttachInfoBarToMainWindow(notification.Message,
                SonarLintImageMoniker.OfficialSonarLintMoniker,
                buttonTexts);
        }
            
        return infoBarManager.AttachInfoBarWithButtons(
            toolWindowId,
            notification.Message,
            buttonTexts,
            SonarLintImageMoniker.OfficialSonarLintMoniker);
    }

    public void Dispose()
    {
        CloseNotification();
    }
}
