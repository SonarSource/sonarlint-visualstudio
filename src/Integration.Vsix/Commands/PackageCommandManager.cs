//-----------------------------------------------------------------------
// <copyright file="PackageCommandManager.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.ComponentModel.Design;
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class PackageCommandManager
    {
        private readonly IServiceProvider serviceProvider;
        private readonly IMenuCommandService menuService;

        public PackageCommandManager(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.serviceProvider = serviceProvider;

            this.menuService = this.serviceProvider.GetService<IMenuCommandService>();
            if (this.menuService == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.MissingService, nameof(IMenuCommandService)), nameof(serviceProvider));
            }
        }

        public void Initialize()
        {
            this.RegisterCommand((int)PackageCommandId.ManageConnections, new ManageConnectionsCommand(this.serviceProvider));
            this.RegisterCommand((int)PackageCommandId.ProjectExcludePropertyToggle, new ProjectExcludePropertyToggleCommand(this.serviceProvider));
            this.RegisterCommand((int)PackageCommandId.ProjectTestPropertyAuto, new ProjectTestPropertySetCommand(this.serviceProvider, null));
            this.RegisterCommand((int)PackageCommandId.ProjectTestPropertyTrue, new ProjectTestPropertySetCommand(this.serviceProvider, true));
            this.RegisterCommand((int)PackageCommandId.ProjectTestPropertyFalse, new ProjectTestPropertySetCommand(this.serviceProvider, false));
        }

        internal /* testing purposes */ OleMenuCommand RegisterCommand(int commandId, VsCommandBase command)
        {
            return this.AddCommand(new Guid(CommonGuids.CommandSet), commandId, command.Invoke, command.QueryStatus);
        }

        private OleMenuCommand AddCommand(Guid commandGroupGuid, int commandId, EventHandler invokeHandler, EventHandler beforeQueryStatus)
        {
            CommandID idObject = new CommandID(commandGroupGuid, commandId);
            OleMenuCommand command = new OleMenuCommand(invokeHandler, delegate { }, beforeQueryStatus, idObject);
            this.menuService.AddCommand(command);
            return command;
        }
    }
}