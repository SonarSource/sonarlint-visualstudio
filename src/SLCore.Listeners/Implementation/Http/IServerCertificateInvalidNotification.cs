/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource SA
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
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Notifications;

namespace SonarLint.VisualStudio.SLCore.Listeners.Implementation.Http;

internal interface IServerCertificateInvalidNotification
{
    void Show();

    void Close();
}

[method: ImportingConstructor]
[Export(typeof(IServerCertificateInvalidNotification))]
[PartCreationPolicy(CreationPolicy.Shared)]
public class ServerCertificateInvalidNotification(
    INotificationService notificationService,
    IOutputWindowService outputWindowService,
    IBrowserService browserService)
    : IServerCertificateInvalidNotification
{
    internal const string ServerCertificateInvalidNotificationId = "ServerCertificateInvalidNotificationId";

    public void Show() => notificationService.ShowNotification(GetServerCertificateInvalidNotification());

    public void Close() => notificationService.CloseNotification();

    private VisualStudio.Core.Notifications.Notification GetServerCertificateInvalidNotification() =>
        new(ServerCertificateInvalidNotificationId,
            SLCoreStrings.ServerCertificateInfobar_CertificateInvalidMessage,
            [
                new NotificationAction(SLCoreStrings.ServerCertificateInfobar_LearnMore, _ => browserService.Navigate(DocumentationLinks.SslCertificate), false),
                new NotificationAction(SLCoreStrings.ServerCertificateInfobar_ShowLogs, _ => outputWindowService.Show(), false)
            ],
            showOncePerSession:false);
}
