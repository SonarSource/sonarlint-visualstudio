//-----------------------------------------------------------------------
// <copyright file="ManageConnectionsCommand.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Vsix
{
    internal class ManageConnectionsCommand : VsCommandBase
    {
        private readonly ITeamExplorerController teamExplorer;

        public ManageConnectionsCommand(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            this.teamExplorer = this.ServiceProvider.GetMefService<ITeamExplorerController>();
            Debug.Assert(this.teamExplorer != null, "Couldn't get Team Explorer controller from MEF");
        }

        protected override void QueryStatusInternal(OleMenuCommand command)
        {
            command.Enabled = (this.teamExplorer != null);
        }

        protected override void InvokeInternal()
        {
            Debug.Assert(this.teamExplorer != null, "Should only be invocable with a handle to the team explorer controller");
            this.teamExplorer.ShowSonarQubePage();
        }
    }
}
