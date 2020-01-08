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
using System.Diagnostics;
using System.Linq;
using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;
using SonarQube.Client.Models;

namespace SonarLint.VisualStudio.Integration.Connection
{
    /// <summary>
    /// Connection related controller.
    /// Provides the following commands:
    /// <see cref="ConnectCommand"/>
    /// <see cref="RefreshCommand"/>
    /// </summary>
    internal sealed class ConnectionController : HostedCommandControllerBase, IConnectionInformationProvider,
        IConnectionWorkflowExecutor
    {
        private readonly IHost host;
        private readonly IConnectionInformationProvider connectionProvider;
        private readonly IProjectSystemHelper projectSystemHelper;

        public ConnectionController(IHost host)
            : this(host, null, null)
        {
        }

        internal /*for testing purposes*/ ConnectionController(IHost host, IConnectionInformationProvider connectionProvider,
            IConnectionWorkflowExecutor workflowExecutor)
            : base(host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.host = host;
            this.WorkflowExecutor = workflowExecutor ?? this;
            this.connectionProvider = connectionProvider ?? this;

            this.projectSystemHelper = this.host.GetService<IProjectSystemHelper>();
            this.projectSystemHelper.AssertLocalServiceIsNotNull();

            this.ConnectCommand = new RelayCommand(this.OnConnect, this.CanConnect);
            this.RefreshCommand = new RelayCommand<ConnectionInformation>(this.OnRefresh, this.CanRefresh);
        }

        #region Properties

        public RelayCommand ConnectCommand
        {
            get;
        }

        public RelayCommand<ConnectionInformation> RefreshCommand
        {
            get;
        }

        internal /*for testing purposes*/ IConnectionWorkflowExecutor WorkflowExecutor
        {
            get;
        }

        internal ConnectionInformation LastAttemptedConnection { get; private set; }

        internal bool IsConnectionInProgress
        {
            get
            {
                return this.host.VisualStateManager.IsBusy;
            }
            set
            {
                if (this.host.VisualStateManager.IsBusy != value)
                {
                    this.host.VisualStateManager.IsBusy = value;
                    this.ConnectCommand.RequeryCanExecute();
                    this.RefreshCommand.RequeryCanExecute();
                }
            }
        }
        #endregion

        #region Connect Command

        private bool CanConnect()
        {
            return this.projectSystemHelper.IsSolutionFullyOpened()
                && !this.host.VisualStateManager.IsConnected
                && !this.host.VisualStateManager.IsBusy;
        }

        private void OnConnect()
        {
            Debug.Assert(this.CanConnect());
            Debug.Assert(!this.host.VisualStateManager.IsBusy, "Service is in a connecting state");

            host.GetMefService<ITelemetryLogger>()?.ReportEvent(TelemetryEvent.ConnectCommandCommandCalled);

            var connectionInfo = this.connectionProvider.GetConnectionInformation(this.LastAttemptedConnection);
            if (connectionInfo != null)
            {
                this.EstablishConnection(connectionInfo);
            }
        }
        #endregion

        #region Refresh Command

        private bool CanRefresh(ConnectionInformation useConnection)
        {
            return !this.host.VisualStateManager.IsBusy
                && (useConnection != null || this.host.VisualStateManager.IsConnected);
        }

        private void OnRefresh(ConnectionInformation useConnection)
        {
            Debug.Assert(this.CanRefresh(useConnection));

            host.GetMefService<ITelemetryLogger>()?.ReportEvent(TelemetryEvent.RefreshCommandCommandCalled);

            // We're currently only connected to one server. when this will change we will need to refresh all the connected servers
            ConnectionInformation connectionToRefresh = useConnection
                ?? this.host.VisualStateManager.GetConnectedServers().FirstOrDefault();
            Debug.Assert(connectionToRefresh != null, "Expecting either to be connected to get a connection to connect to");

            // Any existing connection will be disposed, so create a copy and use it to connect
            this.EstablishConnection(connectionToRefresh.Clone());
        }
        #endregion

        #region IConnectionInformationProvider

        ConnectionInformation IConnectionInformationProvider.GetConnectionInformation(ConnectionInformation currentConnection)
        {
            var dialog = new ConnectionInformationDialog();
            return dialog.ShowDialog(currentConnection);
        }

        #endregion

        #region IConnectionWorkflowExecutor
        private void EstablishConnection(ConnectionInformation connectionInfo)
        {
            Debug.Assert(connectionInfo != null);

            this.LastAttemptedConnection = connectionInfo;

            this.WorkflowExecutor.EstablishConnection(connectionInfo);
        }

        void IConnectionWorkflowExecutor.EstablishConnection(ConnectionInformation information)
        {
            ConnectionWorkflow workflow = new ConnectionWorkflow(this.host, this.ConnectCommand);
            IProgressEvents progressEvents = workflow.Run(information);
            SetConnectionInProgress(progressEvents);
        }

        internal /*for testing purposes*/ void SetConnectionInProgress(IProgressEvents progressEvents)
        {
            this.IsConnectionInProgress = true;

            ProgressNotificationListener progressListener = new ProgressNotificationListener(progressEvents, this.host.Logger);
            progressListener.MessageFormat = Strings.ConnectingToSonarQubePrefixMessageFormat;

            progressEvents.RunOnFinished(result =>
            {
                progressListener.Dispose();
                this.IsConnectionInProgress = false;
            });
        }
        #endregion
    }
}
