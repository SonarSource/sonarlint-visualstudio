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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SonarLint.VisualStudio.Integration.Connection.UI;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.Service.DataModel;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Progress.Controller;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Connection
{
    /// <summary>
    /// Workflow execution for the connect command.
    /// </summary>
    internal class ConnectionWorkflow
    {
        private readonly IHost host;
        private readonly ICommand parentCommand;
        private readonly IProjectSystemHelper projectSystem;

        public ConnectionWorkflow(IHost host, ICommand parentCommand)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (parentCommand == null)
            {
                throw new ArgumentNullException(nameof(parentCommand));
            }

            this.host = host;
            this.parentCommand = parentCommand;
            this.projectSystem = this.host.GetService<IProjectSystemHelper>();
            this.projectSystem.AssertLocalServiceIsNotNull();
        }

        internal /*for testing purposes*/ ConnectionInformation ConnectedServer
        {
            get;
            set;
        }

        #region Start the workflow
        public IProgressEvents Run(ConnectionInformation information)
        {
            Debug.Assert(this.host.ActiveSection != null, "Expect the section to be attached at least until this method returns");

            this.OnProjectsChanged(information, null);
            IProgressEvents progress = ProgressStepRunner.StartAsync(this.host, this.host.ActiveSection.ProgressHost, (controller) => this.CreateConnectionSteps(controller, information));
            this.DebugOnly_MonitorProgress(progress);
            return progress;
        }

        [Conditional("DEBUG")]
        private void DebugOnly_MonitorProgress(IProgressEvents progress)
        {
            progress.RunOnFinished(r => VsShellUtils.WriteToSonarLintOutputPane(this.host, "DEBUGONLY: Connect workflow finished, Execution result: {0}", r));
        }

        private ProgressStepDefinition[] CreateConnectionSteps(IProgressController controller, ConnectionInformation connection)
        {
            string connectStepDisplayText = string.Format(CultureInfo.CurrentCulture, Strings.ConnectingToSever,
                connection.ServerUri);
            return new[]
            {
                new ProgressStepDefinition(connectStepDisplayText, StepAttributes.Indeterminate | StepAttributes.BackgroundThread,
                    async (cancellationToken, notifications) =>
                        await this.ConnectionStepAsync(connection, controller, notifications, cancellationToken)),

                new ProgressStepDefinition(connectStepDisplayText, StepAttributes.BackgroundThread,
                    async (token, notifications) =>
                        await this.DownloadServiceParametersAsync(controller, notifications, token)),

                };
        }

        #endregion

        #region Workflow events

        private void OnProjectsChanged(ConnectionInformation connection, IEnumerable<SonarQubeProject> projects)
        {
            this.host.VisualStateManager.SetProjects(connection, projects);
        }

        #endregion

        #region Workflow steps

        internal /* for testing purposes */ async Task ConnectionStepAsync(ConnectionInformation connection,
            IProgressController controller, IProgressStepExecutionEvents notifications, CancellationToken cancellationToken)
        {
            this.host.ActiveSection?.UserNotifications?.HideNotification(NotificationIds.FailedToConnectId);
            this.host.ActiveSection?.UserNotifications?.HideNotification(NotificationIds.BadSonarQubePluginId);

            notifications.ProgressChanged(connection.ServerUri.ToString());

            try
            {
                notifications.ProgressChanged(Strings.ConnectionStepValidatinCredentials);
                await this.host.SonarQubeService.ConnectAsync(connection, cancellationToken); // TODO: Handle errors

                if (connection.Organization == null &&
                    this.host.SonarQubeService.HasOrganizationsFeature)
                {
                    notifications.ProgressChanged(Strings.ConnectionStepRetrievingOrganizations);
                    var organizations = await this.host.SonarQubeService.GetAllOrganizationsAsync(cancellationToken);

                    if (organizations.Count <= 1) // Only 1 org means the SQ server is recent enough but the feature is not active
                    {
                        connection.Organization = null;
                    }
                    else
                    {
                        connection.Organization = AskUserToSelectOrganizationOnUIThread(organizations);
                        if (connection.Organization == null) // User clicked cancel
                        {
                            AbortWithMessage(notifications, controller, cancellationToken); // TODO: Might be worth throwing
                            return;
                        }

                    }
                }

                var isCompatible = await this.AreSolutionProjectsAndSonarQubePluginsCompatible(controller, notifications,
                    cancellationToken);
                if (!isCompatible)
                {
                    return; // Message is already displayed by the method
                }

                this.ConnectedServer = connection;

                notifications.ProgressChanged(Strings.ConnectionStepRetrievingProjects);
                var projects = await this.host.SonarQubeService.GetAllProjectsAsync(connection.Organization.Key,
                    cancellationToken);

                this.OnProjectsChanged(connection, projects);
                notifications.ProgressChanged(Strings.ConnectionResultSuccess);
            }
            catch (HttpRequestException e)
            {
                // For some errors we will get an inner exception which will have a more specific information
                // that we would like to show i.e.when the host could not be resolved
                var innerException = e.InnerException as System.Net.WebException;
                VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarQubeRequestFailed, e.Message, innerException?.Message);
                AbortWithMessage(notifications, controller, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Canceled or timeout
                VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarQubeRequestTimeoutOrCancelled);
                AbortWithMessage(notifications, controller, cancellationToken);
            }
            catch (Exception ex)
            {
                VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SonarQubeRequestFailed, ex.Message, null);
                AbortWithMessage(notifications, controller, cancellationToken);
            }
        }

        private void AbortWithMessage(IProgressStepExecutionEvents notifications, IProgressController controller,
            CancellationToken cancellationToken)
        {
            notifications.ProgressChanged(cancellationToken.IsCancellationRequested
                ? Strings.ConnectionResultCancellation
                : Strings.ConnectionResultFailure);
            this.host.ActiveSection?.UserNotifications?.ShowNotificationError(Strings.ConnectionFailed,
                NotificationIds.FailedToConnectId, this.parentCommand);

            AbortWorkflow(controller, cancellationToken);
        }

        private Organization AskUserToSelectOrganizationOnUIThread(IEnumerable<Organization> organizations)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                var organizationDialog = new OrganizationSelectionWindow(organizations) { Owner = Application.Current.MainWindow };
                var hasUserClickedOk = organizationDialog.ShowDialog().GetValueOrDefault();

                return hasUserClickedOk ? organizationDialog.GetSelectedOrganization() : null;
            });
        }

        internal /*for testing purposes*/ async Task DownloadServiceParametersAsync(IProgressController controller,
            IProgressStepExecutionEvents notifications, CancellationToken token)
        {
            Debug.Assert(this.ConnectedServer != null);

            const string TestProjectRegexKey = "sonar.cs.msbuild.testProjectPattern";
            const string TestProjectRegexDefaultValue = @"[^\\]*test[^\\]*$";

        // Should never realistically take more than 1 second to match against a project name
        var timeout = TimeSpan.FromSeconds(1);
            var defaultRegex = new Regex(TestProjectRegexDefaultValue, RegexOptions.IgnoreCase, timeout);

            notifications.ProgressChanged(Strings.DownloadingServerSettingsProgessMessage);

            var properties = await this.host.SonarQubeService.GetAllPropertiesAsync(token);
            if (token.IsCancellationRequested)
            {
                AbortWorkflow(controller, token);
                return;
            }

            var testProjRegexPattern = properties.First(x => StringComparer.Ordinal.Equals(x.Key, TestProjectRegexKey)).Value;

            Regex regex = null;
            if (testProjRegexPattern != null)
            {
                // Try and create regex from provided server pattern.
                // No way to determine a valid pattern other than attempting to construct
                // the Regex object.
                try
                {
                    regex = new Regex(testProjRegexPattern, RegexOptions.IgnoreCase, timeout);
                }
                catch (ArgumentException)
                {
                    VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.InvalidTestProjectRegexPattern,
                        testProjRegexPattern);
                }
            }

            var projectFilter = this.host.GetService<IProjectSystemFilter>();
            projectFilter.AssertLocalServiceIsNotNull();
            projectFilter.SetTestRegex(regex ?? defaultRegex);
        }

        #endregion

        #region Helpers

        private async Task<bool> AreSolutionProjectsAndSonarQubePluginsCompatible(IProgressController controller,
            IProgressStepExecutionEvents notifications, CancellationToken cancellationToken)
        {
            notifications.ProgressChanged(Strings.DetectingSonarQubePlugins);

            var plugins = await this.host.SonarQubeService.GetAllPluginsAsync(cancellationToken);

            var csharpOrVbNetProjects = new HashSet<EnvDTE.Project>(this.projectSystem.GetSolutionProjects());
            var supportedSonarQubePlugins = MinimumSupportedSonarQubePlugin.All
                .Where(lang => IsSonarQubePluginSupported(plugins, lang))
                .Select(lang => lang.Language);
            this.host.SupportedPluginLanguages.UnionWith(new HashSet<Language>(supportedSonarQubePlugins));

            // If any of the project can be bound then return success
            if (csharpOrVbNetProjects.Select(Language.ForProject)
                                     .Any(this.host.SupportedPluginLanguages.Contains))
            {
                return true;
            }

            string errorMessage = GetPluginProjectMismatchErrorMessage(csharpOrVbNetProjects);
            this.host.ActiveSection?.UserNotifications?.ShowNotificationError(errorMessage, NotificationIds.BadSonarQubePluginId, null);
            VsShellUtils.WriteToSonarLintOutputPane(this.host, Strings.SubTextPaddingFormat, errorMessage);
            notifications.ProgressChanged(Strings.ConnectionResultFailure);

            AbortWorkflow(controller, cancellationToken);
            return false;
        }

        private string GetPluginProjectMismatchErrorMessage(ICollection<EnvDTE.Project> csharpOrVbNetProjects)
        {
            if (this.host.SupportedPluginLanguages.Count == 0)
            {
                return Strings.ServerHasNoSupportedPluginVersion;
            }

            if (csharpOrVbNetProjects.Count == 0)
            {
                return Strings.SolutionContainsNoSupportedProject;
            }

            return string.Format(Strings.OnlySupportedPluginHasNoProjectInSolution, this.host.SupportedPluginLanguages.First().Name);
        }

        private bool IsSonarQubePluginSupported(IEnumerable<SonarQubePlugin> plugins, MinimumSupportedSonarQubePlugin minimumSupportedPlugin)
        {
            var plugin = plugins.FirstOrDefault(x => StringComparer.Ordinal.Equals(x.Key, minimumSupportedPlugin.Key));
            var isPluginSupported = !string.IsNullOrWhiteSpace(plugin?.Version) && VersionHelper.Compare(plugin.Version, minimumSupportedPlugin.MinimumVersion) >= 0;

            var pluginSupportMessageFormat = string.Format(Strings.SubTextPaddingFormat, isPluginSupported ? Strings.SupportedPluginFoundMessage : Strings.UnsupportedPluginFoundMessage);
            VsShellUtils.WriteToSonarLintOutputPane(this.host, pluginSupportMessageFormat, minimumSupportedPlugin.ToString());

            return isPluginSupported;
        }

        private static void AbortWorkflow(IProgressController controller, CancellationToken token)
        {
            bool aborted = controller.TryAbort();
            Debug.Assert(aborted || token.IsCancellationRequested, "Failed to abort the workflow");
        }

        #endregion
    }
}
