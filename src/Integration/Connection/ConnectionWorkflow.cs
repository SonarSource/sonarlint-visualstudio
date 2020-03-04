/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2020 SonarSource SA
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
using Microsoft.Alm.Authentication;
using SonarLint.VisualStudio.Core;
using SonarLint.VisualStudio.Integration.Connection.UI;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Progress.Controller;
using SonarQube.Client.Helpers;
using SonarQube.Client.Models;
using Language = SonarLint.VisualStudio.Core.Language;

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
        private readonly ICredentialStoreService credentialStore;

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
            this.credentialStore = this.host.GetService<ICredentialStoreService>();
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

#if DEBUG
            progress.RunOnFinished(r => this.host.Logger.WriteLine("DEBUGONLY: Connect workflow finished, Execution result: {0}", r));
#endif

            return progress;
        }

        private ProgressStepDefinition[] CreateConnectionSteps(IProgressController controller, ConnectionInformation connection)
        {
            string connectStepDisplayText = string.Format(CultureInfo.CurrentCulture, Strings.ConnectingToSever,
                connection.ServerUri);
            return new[]
            {
                new ProgressStepDefinition(connectStepDisplayText, StepAttributes.Indeterminate | StepAttributes.BackgroundThread,
                    (cancellationToken, notifications) =>
                    this.ConnectionStepAsync(connection, controller, notifications, cancellationToken).GetAwaiter().GetResult()),

                new ProgressStepDefinition(connectStepDisplayText, StepAttributes.BackgroundThread,
                    (token, notifications) =>
                    this.DownloadServiceParametersAsync(controller, notifications, token).GetAwaiter().GetResult()),

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
                if (!this.host.SonarQubeService.IsConnected)
                {
                    await this.host.SonarQubeService.ConnectAsync(connection, cancellationToken);
                }

                if (connection.Organization == null)
                {
                    var hasOrgs = await this.host.SonarQubeService.HasOrganizations(cancellationToken);
                    if (hasOrgs)
                    {
                        notifications.ProgressChanged(Strings.ConnectionStepRetrievingOrganizations);
                        var organizations = await this.host.SonarQubeService.GetAllOrganizationsAsync(cancellationToken);

                        connection.Organization = AskUserToSelectOrganizationOnUIThread(organizations);
                        if (connection.Organization == null) // User clicked cancel
                        {
                            AbortWithMessage(notifications, controller, cancellationToken); // TODO: Might be worth throwing
                            return;
                        }
                    }

                }

                // Persist the credentials on successful connection to SonarQube, unless
                // the connection is anonymous
                if (!string.IsNullOrEmpty(connection.UserName) &&
                    !string.IsNullOrEmpty(connection.Password.ToUnsecureString()))
                {
                    this.credentialStore.WriteCredentials(
                        connection.ServerUri,
                        new Credential(connection.UserName, connection.Password.ToUnsecureString()));
                }

                var isCompatible = await this.AreSolutionProjectsAndSonarQubePluginsCompatibleAsync(controller, notifications,
                    cancellationToken);
                if (!isCompatible)
                {
                    return; // Message is already displayed by the method
                }

                this.ConnectedServer = connection;

                notifications.ProgressChanged(Strings.ConnectionStepRetrievingProjects);
                var projects = await this.host.SonarQubeService.GetAllProjectsAsync(connection.Organization?.Key,
                    cancellationToken);

                // The SonarQube client will limit the number of returned projects to 10K (hard limit on SonarQube side)
                // but will no longer fail when trying to retrieve more. In the case where the project we want to bind to
                // is not in the list and the binding was already done and the key is not null then we manually
                // forge and add a new project to the list.
                if (!string.IsNullOrWhiteSpace(this.host.VisualStateManager.BoundProjectKey) &&
                    projects.Count == 10000 &&
                    !projects.Any(p => p.Key == this.host.VisualStateManager.BoundProjectKey))
                {
                    this.host.Logger.WriteLine($"The project with key '{this.host.VisualStateManager.BoundProjectKey}' is not part of the first ten thousand projects. The binding process will continue assuming it was found.");
                    this.host.Logger.WriteLine("Note that if the project key does not actually exist on the server the binding will fail at a later stage.");

                    // We have to create a new list because the collection returned by the service as a fixed size
                    projects = new List<SonarQubeProject>(projects);
                    // Let's put the new item first in the collection to ease finding it.
                    projects.Insert(0, new SonarQubeProject(this.host.VisualStateManager.BoundProjectKey,
                        this.host.VisualStateManager.BoundProjectName ?? this.host.VisualStateManager.BoundProjectKey));
                }

                this.OnProjectsChanged(connection, projects);
                notifications.ProgressChanged(Strings.ConnectionResultSuccess);
            }
            catch (HttpRequestException e)
            {
                // For some errors we will get an inner exception which will have a more specific information
                // that we would like to show i.e.when the host could not be resolved
                var innerException = e.InnerException as System.Net.WebException;
                this.host.Logger.WriteLine(CoreStrings.SonarQubeRequestFailed, e.Message, innerException?.Message);
                AbortWithMessage(notifications, controller, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                // Canceled or timeout
                this.host.Logger.WriteLine(CoreStrings.SonarQubeRequestTimeoutOrCancelled);
                AbortWithMessage(notifications, controller, cancellationToken);
            }
            catch (Exception ex)
            {
                this.host.Logger.WriteLine(CoreStrings.SonarQubeRequestFailed, ex.Message, null);
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

        private SonarQubeOrganization AskUserToSelectOrganizationOnUIThread(IEnumerable<SonarQubeOrganization> organizations)
        {
            return Application.Current.Dispatcher.Invoke(() =>
            {
                var organizationDialog = new OrganizationSelectionWindow(organizations) { Owner = Application.Current.MainWindow };
                var hasUserClickedOk = organizationDialog.ShowDialog().GetValueOrDefault();

                return hasUserClickedOk ? organizationDialog.Organization : null;
            });
        }

        internal /*for testing purposes*/ async Task DownloadServiceParametersAsync(IProgressController controller,
            IProgressStepExecutionEvents notifications, CancellationToken token)
        {
            Debug.Assert(this.ConnectedServer != null);

            notifications.ProgressChanged(Strings.DownloadingServerSettingsProgessMessage);

            var properties = await this.host.SonarQubeService.GetAllPropertiesAsync(this.host.VisualStateManager.BoundProjectKey, token);
            if (token.IsCancellationRequested)
            {
                AbortWorkflow(controller, token);
                return;
            }

            var testProjRegexPattern = properties.FirstOrDefault(IsTestProjectPatternProperty)?.Value;

            if (testProjRegexPattern != null)
            {
                // Try and create regex from provided server pattern.
                // No way to determine a valid pattern other than attempting to construct
                // the Regex object.
                try
                {
                    Regex.IsMatch("", testProjRegexPattern);

                    var projectFilter = this.host.GetService<IProjectSystemFilter>();
                    projectFilter.AssertLocalServiceIsNotNull();
                    projectFilter.SetTestRegex(testProjRegexPattern);
                }
                catch (ArgumentException)
                {
                    this.host.Logger.WriteLine(Strings.InvalidTestProjectRegexPattern, testProjRegexPattern);
                }
            }
        }

        private static bool IsTestProjectPatternProperty(SonarQubeProperty property)
        {
            const string TestProjectRegexKey = "sonar.cs.msbuild.testProjectPattern";
            return StringComparer.Ordinal.Equals(property.Key, TestProjectRegexKey);
        }

        #endregion

        #region Helpers

        private async Task<bool> AreSolutionProjectsAndSonarQubePluginsCompatibleAsync(IProgressController controller,
            IProgressStepExecutionEvents notifications, CancellationToken cancellationToken)
        {
            notifications.ProgressChanged(Strings.DetectingSonarQubePlugins);

            var plugins = await this.host.SonarQubeService.GetAllPluginsAsync(cancellationToken);

            var csharpOrVbNetProjects = new HashSet<EnvDTE.Project>(this.projectSystem.GetSolutionProjects());
            var supportedPluginsLanguages = MinimumSupportedSonarQubePlugin.All
                .Where(supportedPlugin => IsSonarQubePluginSupported(plugins, supportedPlugin, host.Logger))
                .SelectMany(lang => lang.Languages);
            this.host.SupportedPluginLanguages.UnionWith(new HashSet<Language>(supportedPluginsLanguages));

            // If any of the projects can be bound then return success
            if (csharpOrVbNetProjects.Select(ProjectToLanguageMapper.GetLanguageForProject)
                                     .Any(this.host.SupportedPluginLanguages.Contains))
            {
                return true;
            }

            string errorMessage = GetPluginProjectMismatchErrorMessage(csharpOrVbNetProjects);
            this.host.ActiveSection?.UserNotifications?.ShowNotificationError(errorMessage, NotificationIds.BadSonarQubePluginId, null);
            this.host.Logger.WriteLine(Strings.SubTextPaddingFormat, errorMessage);
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

            var supportedPluginsNames = string.Join(", ", this.host.SupportedPluginLanguages.Select(p => p.Name));
            return string.Format(Strings.OnlySupportedPluginsHaveNoProjectInSolution, supportedPluginsNames);
        }

        internal static /*for testing purposes*/ bool IsSonarQubePluginSupported(IEnumerable<SonarQubePlugin> installedPlugins,
            MinimumSupportedSonarQubePlugin minimumSupportedPlugin, ILogger logger)
        {
            var installedPlugin = installedPlugins.FirstOrDefault(x => StringComparer.Ordinal.Equals(x.Key, minimumSupportedPlugin.Key));

            if (installedPlugin == null) // plugin is not installed on remote server
            {
                return false;
            }

            var languageNames = string.Join(", ", minimumSupportedPlugin.Languages.Select(l => l.Name));
            var pluginInfoMessage = string.Format(CultureInfo.CurrentCulture, Strings.InstalledAndMinimumSonarQubePlugin,
                minimumSupportedPlugin.PluginName, languageNames, installedPlugin.Version, minimumSupportedPlugin.MinimumVersion);

            var isPluginSupported = !string.IsNullOrWhiteSpace(installedPlugin.Version) &&
                VersionHelper.Compare(installedPlugin.Version, minimumSupportedPlugin.MinimumVersion) >= 0;

            // Let's handle specific case for old Visual Studio instances
            if (isPluginSupported &&
                VisualStudioHelpers.IsVisualStudioBeforeUpdate3())
            {
                if (minimumSupportedPlugin == MinimumSupportedSonarQubePlugin.CSharp)
                {
                    const string newRoslynSonarCSharpVersion = "7.0";
                    pluginInfoMessage += $", Maximum version: '{newRoslynSonarCSharpVersion}'";
                    isPluginSupported = VersionHelper.Compare(installedPlugin.Version, newRoslynSonarCSharpVersion) < 0;
                }

                if (minimumSupportedPlugin == MinimumSupportedSonarQubePlugin.VbNet)
                {
                    const string newRoslynSonarVBNetVersion = "5.0";
                    pluginInfoMessage += $", Maximum version: '{newRoslynSonarVBNetVersion}'";
                    isPluginSupported = VersionHelper.Compare(installedPlugin.Version, newRoslynSonarVBNetVersion) < 0;
                }
            }

            var pluginSupportMessageFormat = string.Format(CultureInfo.CurrentCulture, Strings.SubTextPaddingFormat,
                isPluginSupported
                    ? Strings.SupportedPluginFoundMessage
                    : Strings.UnsupportedPluginFoundMessage);
            logger.WriteLine(pluginSupportMessageFormat, pluginInfoMessage);

            return isPluginSupported;
        }

        private void AbortWorkflow(IProgressController controller, CancellationToken token)
        {
            bool aborted = controller.TryAbort();
            Debug.Assert(aborted || token.IsCancellationRequested, "Failed to abort the workflow");

            this.host.SonarQubeService.Disconnect();
        }

        #endregion
    }
}
