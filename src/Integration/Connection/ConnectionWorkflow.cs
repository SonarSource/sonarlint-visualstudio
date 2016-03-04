//-----------------------------------------------------------------------
// <copyright file="ConnectionWorkflow.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Progress.Controller;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Input;

namespace SonarLint.VisualStudio.Integration.Connection
{
    /// <summary>
    /// Workflow execution for the connect command.
    /// </summary>
    internal class ConnectionWorkflow
    {
        private readonly IHost host;
        private readonly ICommand parentCommand;
        private readonly ConnectedProjectsCallback connectedProjectsChanged;

        public ConnectionWorkflow(IHost host, ICommand parentCommand, ConnectedProjectsCallback connectedProjectsChanged)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (parentCommand == null)
            {
                throw new ArgumentNullException(nameof(parentCommand));
            }

            if (connectedProjectsChanged == null)
            {
                throw new ArgumentNullException(nameof(connectedProjectsChanged));
            }

            this.host = host;
            this.parentCommand = parentCommand;
            this.connectedProjectsChanged = connectedProjectsChanged;
        }

        #region Start the workflow
        public IProgressEvents Run(ConnectionInformation information)
        {
            Debug.Assert(this.host.ActiveSection != null, "Expect the section to be attached at least until this method returns");
            Debug.Assert(!ReferenceEquals(information, this.host.SonarQubeService.CurrentConnection), "Using the same connection instance - it will be disposed during the execution, so clone it before calling this method");

            this.OnProjectsChanged(information, null);
            IProgressEvents progress = ProgressStepRunner.StartAsync(this.host, this.host.ActiveSection.ProgressHost, (controller) => this.CreateConnectionSteps(controller, information));
            this.DebugOnly_MonitorProgress(progress);
            return progress;
        }

        [Conditional("DEBUG")]
        private void DebugOnly_MonitorProgress(IProgressEvents progress)
        {
            progress.RunOnFinished(r => VsShellUtils.WriteToGeneralOutputPane(this.host, "DEBUGONLY: Connect workflow finished, Execution result: {0}", r));
        }

        private ProgressStepDefinition[] CreateConnectionSteps(IProgressController controller, ConnectionInformation connection)
        {
            string connectStepDisplayText = string.Format(CultureInfo.CurrentCulture, Resources.Strings.ConnectingToSever, connection.ServerUri);
            return new[]
            {
                    new ProgressStepDefinition(connectStepDisplayText, StepAttributes.Indeterminate | StepAttributes.BackgroundThread, (cancellationToken, notifications) =>
                    {
                        this.ConnectionStep(controller, cancellationToken, connection, notifications);
                    }),
                };
        }

        #endregion

        #region Workflow events

        private void OnProjectsChanged(ConnectionInformation connection, ProjectInformation[] projects)
        {
            this.connectedProjectsChanged.Invoke(connection, projects);
        }

        #endregion

        #region Workflow steps

        internal /* for testing purposes */ void ConnectionStep(IProgressController controller, CancellationToken cancellationToken, ConnectionInformation connection, IProgressStepExecutionEvents notifications)
        {
            this.host.ActiveSection?.UserNotifications?.HideNotification(NotificationIds.FailedToConnectId);

            notifications.ProgressChanged(connection.ServerUri.ToString(), double.NaN);

            ProjectInformation[] projects = this.host.SonarQubeService.Connect(connection, cancellationToken)?.ToArray();
            this.OnProjectsChanged(connection, projects);

            if (this.host.SonarQubeService.CurrentConnection == null)
            {
                notifications.ProgressChanged(cancellationToken.IsCancellationRequested ? Strings.ConnectionResultCancellation : Strings.ConnectionResultFailure, double.NaN);

                bool aborted = controller.TryAbort();
                Debug.Assert(aborted || cancellationToken.IsCancellationRequested, "Failed to abort the workflow");

                this.host.ActiveSection?.UserNotifications?.ShowNotificationError(Strings.ConnectionFailed, NotificationIds.FailedToConnectId, this.parentCommand);
            }
            else
            {
                notifications.ProgressChanged(Strings.ConnectionResultSuccess, double.NaN);
            }
        }

        #endregion
    }
}
