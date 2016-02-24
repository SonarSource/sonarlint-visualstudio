//-----------------------------------------------------------------------
// <copyright file="TestableConnectSectionController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Connection;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using System;
using System.Windows.Threading;

namespace SonarLint.VisualStudio.Integration.UnitTests.TeamExplorer
{
    internal class TestableConnectSectionController : ConnectSectionController
    {
        public TestableConnectSectionController(IServiceProvider serviceProvider,
                                               ISonarQubeServiceWrapper sonarQubeService)
            : this(serviceProvider, sonarQubeService, new ConfigurableActiveSolutionTracker())
        {
        }

        public TestableConnectSectionController(IServiceProvider serviceProvider,
                                                ISonarQubeServiceWrapper sonarQubeService,
                                                IActiveSolutionTracker tracker)
            : base(serviceProvider, sonarQubeService, tracker, Dispatcher.CurrentDispatcher)
        {
        }

        public Action SetProjectsAction { get; set; }

        public ConfigurableUserNotification NotificationOverride
        {
            get;
        } = new ConfigurableUserNotification();

        #region Overrides
        internal override IUserNotification Notification
        {
            get
            {
                return this.NotificationOverride;
            }
        }

        internal protected override void SetProjects(object sender, ConnectedProjectsEventArgs args)
        {
            if (this.SetProjectsAction != null)
            {
                this.SetProjectsAction();
                return;
            }

            // This call will issue an async request
            base.SetProjects(sender, args);

            // This call will make sure that that async request is executed now 
            DispatcherHelper.DispatchFrame();
        }
        #endregion
    }
}
