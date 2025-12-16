/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2025 SonarSource Sàrl
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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Core.Binding;
using SonarLint.VisualStudio.Core.SmartNotification;
using SonarLint.VisualStudio.Integration.MefServices;
using SonarLint.VisualStudio.Integration.Notifications;
using SonarLint.VisualStudio.Integration.Vsix.Resources;
using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    [ExcludeFromCodeCoverage] // Not easily testable, this class should be kept as simple as possible
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.FolderOpened_string, PackageAutoLoadFlags.BackgroundLoad)]
    public sealed class SonarLintNotificationsPackage : AsyncPackage
    {
        /// <summary>
        /// SonarLintNotifications GUID string.
        /// </summary>
        public const string PackageGuidString = "c26b6802-dd9c-4a49-b8a5-0ad8ef04c579";

        private NotificationIndicator notificationIcon;

        private IActiveSolutionBoundTracker activeSolutionBoundTracker;
        private INotificationIndicatorViewModel notificationIndicatorViewModel;
        private ILogger logger;
        private bool disposed;
        private ISharedBindingSuggestionService suggestSharedBindingGoldBar;

        protected override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            JoinableTaskFactory.RunAsync(InitAsync);
            return Task.CompletedTask;
        }

        private async Task InitAsync()
        {
            // Working on background thread...
            Debug.Assert(!ThreadHelper.CheckAccess());

            logger = await this.GetMefServiceAsync<ILogger>();
            logger.WriteLine(Strings.Notifications_Initializing);

            var vsBrowserService = await this.GetMefServiceAsync<IBrowserService>();

            // Initializing the UI elements has to be on the main thread
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            SafePerformOpOnUIThread(() =>
            {
                // Creating the tracker might indirectly cause UI-related MEF components to be
                // created, so this needs to be done on the UI thread just in case.
                activeSolutionBoundTracker = this.GetMefService<IActiveSolutionBoundTracker>();
                var smartNotificationService = this.GetMefService<ISmartNotificationService>();
                notificationIndicatorViewModel = new NotificationIndicatorViewModel(smartNotificationService, vsBrowserService, activeSolutionBoundTracker);

                PerformUIInitialisation();
                logger.WriteLine(Strings.Notifications_InitializationComplete);

                suggestSharedBindingGoldBar = this.GetMefService<ISharedBindingSuggestionService>();
                suggestSharedBindingGoldBar.Suggest();
            });
        }

        private void PerformUIInitialisation()
        {
            notificationIcon = new NotificationIndicator { DataContext = notificationIndicatorViewModel };

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

            if (bindingConfiguration.Mode != SonarLintMode.Standalone)
            {
                logger.WriteLine(Strings.Notifications_Connected);
                VisualStudioStatusBarHelper.AddStatusBarIcon(notificationIcon);
            }
            else
            {
                logger.WriteLine(Strings.Notifications_NotConnected);
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

            suggestSharedBindingGoldBar?.Dispose();
            disposed = true;
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
                logger.WriteLine(Strings.Notifications_ERROR, ex.Message);
            }
        }
    }
}
