/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2018 SonarSource SA
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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration.NewConnectedMode;
using SonarLint.VisualStudio.Integration.Notifications;
using SonarQube.Client;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [ExcludeFromCodeCoverage] // Not easily testable, this class should be kept as simple as possible
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class SonarLintNotificationsPackage : AsyncPackage
    {
        /// <summary>
        /// SonarLintNotifications GUID string.
        /// </summary>
        public const string PackageGuidString = "c26b6802-dd9c-4a49-b8a5-0ad8ef04c579";
        private const string NotificationDataKey = "NotificationEventData";

        private readonly IFormatter formatter = new BinaryFormatter();
        private NotificationIndicator notificationIcon;

        private IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private ISonarQubeNotificationService notifications;
        private ILogger logger;
        private NotificationData notificationData;
        private bool disposed;

        protected override System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            JoinableTaskFactory.RunAsync(InitAsync);
            return System.Threading.Tasks.Task.CompletedTask;
        }

        private async System.Threading.Tasks.Task InitAsync()
        {
            // Working on background thread...
            Debug.Assert(!ThreadHelper.CheckAccess());

            var sonarqubeService = await this.GetMefServiceAsync<ISonarQubeService>();
            logger = await this.GetMefServiceAsync<ILogger>();
            logger.WriteLine(Resources.Strings.Notifications_Initializing);

            AddOptionKey(NotificationDataKey);

            notifications = new SonarQubeNotificationService(sonarqubeService,
                new NotificationIndicatorViewModel(), new TimerWrapper { Interval = 60000 }, logger);

            activeSolutionBoundTracker = await this.GetMefServiceAsync<IActiveSolutionBoundTracker>();

            // A bound solution might already have completed loading. If so, we need to
            // trigger the load of the options from the solution file
            if (activeSolutionBoundTracker.CurrentConfiguration.Mode != SonarLintMode.Standalone)
            {
                var solutionPersistence = (IVsSolutionPersistence)await this.GetServiceAsync(typeof(SVsSolutionPersistence));
                solutionPersistence.LoadPackageUserOpts(this, NotificationDataKey);
            }

            // Initialising the UI elements has to be on the main thread
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            SafePerformOpOnUIThread(() =>
            {
                PerformUIInitialisation();
                logger.WriteLine(Resources.Strings.Notifications_InitializationComplete);
            });
        }
        private void PerformUIInitialisation()
        {
            notificationIcon = new NotificationIndicator();
            notificationIcon.DataContext = notifications.Model;

            // The package is now loaded asynchronously, so a solution might have
            // finished loading before this package is initialized
            Refresh(activeSolutionBoundTracker.CurrentConfiguration);

            // Ordering: don't register for solution change notifications
            // until we've finished setting up everything else
            activeSolutionBoundTracker.SolutionBindingChanged += OnSolutionBindingChanged;
        }

        private void OnSolutionBindingChanged(object sender, ActiveSolutionBindingEventArgs binding)
        {
            SafePerformOpOnUIThread(() => Refresh(binding.Configuration));
        }

        private void Refresh(BindingConfiguration bindingConfiguration)
        {
            Debug.Assert(ThreadHelper.CheckAccess());

            if (bindingConfiguration.Mode != NewConnectedMode.SonarLintMode.Standalone)
            {
                logger.WriteLine(Resources.Strings.Notifications_Connected);
                VisualStudioStatusBarHelper.AddStatusBarIcon(notificationIcon);
                notifications.StartAsync(bindingConfiguration.Project.ProjectKey, notificationData);
            }
            else
            {
                logger.WriteLine(Resources.Strings.Notifications_NotConnected);
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
                logger.WriteLine(Resources.Strings.Notifications_SavingSettings);
                formatter.Serialize(stream, notifications.GetNotificationData());
            }
        }

        protected override void OnLoadOptions(string key, Stream stream)
        {
            // Note: when the package is loaded asynchronously, a solution might
            // already be loaded in which case this method will not be called by VS.
            // In that case we manually trigger the load using the IVsSolutionPersistence
            // service.
            if (key == NotificationDataKey)
            {
                try
                {
                    logger.WriteLine(Resources.Strings.Notifications_LoadingSettings);
                    notificationData = formatter.Deserialize(stream) as NotificationData;
                }
                catch (Exception ex)
                {
                    logger.WriteLine($"Failed to read notification data: {ex.Message}");
                }
            }
        }

        private void SafePerformOpOnUIThread(Action op)
        {
            Debug.Assert(ThreadHelper.CheckAccess());

            try
            {
                op();
            }
            catch (Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine(Resources.Strings.Notifications_ERROR, ex.Message);
            }
        }
    }
}
