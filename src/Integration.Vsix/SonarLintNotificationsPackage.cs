/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2017 SonarSource SA
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

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.Notifications;
using SonarQube.Client.Services;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [ExcludeFromCodeCoverage] // Not easily testable, this class should be kept as simple as possible
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(UIContextGuids.NoSolution)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists)]
    public sealed class SonarLintNotificationsPackage : Package
    {
        /// <summary>
        /// SonarLintNotifications GUID string.
        /// </summary>
        public const string PackageGuidString = "c26b6802-dd9c-4a49-b8a5-0ad8ef04c579";
        private const string NotificationDataKey = "NotificationEventData";

        private readonly IFormatter formatter = new BinaryFormatter();
        private readonly NotificationIndicator notificationIcon = new NotificationIndicator();

        private IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private ISonarQubeNotificationService notifications;
        private ISonarLintOutput sonarLintOutput;
        private NotificationData notificationData;
        private bool disposed;

        protected override void Initialize()
        {
            AddOptionKey(NotificationDataKey);
            base.Initialize();

            var sonarqubeService = this.GetMefService<ISonarQubeService>();
            sonarLintOutput = this.GetMefService<ISonarLintOutput>();

            notifications = new SonarQubeNotificationService(sonarqubeService,
                new NotificationIndicatorViewModel(), new TimerWrapper { Interval = 60000 }, sonarLintOutput);

            notificationIcon.DataContext = notifications.Model;

            activeSolutionBoundTracker = this.GetMefService<IActiveSolutionBoundTracker>();
            activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;
        }

        private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs binding)
        {
            if (binding.IsBound)
            {
                VisualStudioStatusBarHelper.AddStatusBarIcon(notificationIcon);
                notifications.StartAsync(binding.ProjectKey, notificationData);
            }
            else
            {
                notifications.Stop();
                VisualStudioStatusBarHelper.RemoveStatusBarIcon(notificationIcon);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            base.Dispose(disposing);

            activeSolutionBoundTracker.SolutionBindingChanged -= OnSolutionBindingChanged;

            (notifications as IDisposable)?.Dispose();
            disposed = true;
        }

        protected override void OnSaveOptions(string key, Stream stream)
        {
            if (key == NotificationDataKey)
            {
                formatter.Serialize(stream, notifications.GetNotificationData());
            }
        }

        protected override void OnLoadOptions(string key, Stream stream)
        {
            if (key == NotificationDataKey)
            {
                try
                {
                    notificationData = formatter.Deserialize(stream) as NotificationData;
                }
                catch (Exception ex)
                {
                    sonarLintOutput.Write($"Failed to read notification data: {ex.Message}");
                }
            }
        }
    }
}
