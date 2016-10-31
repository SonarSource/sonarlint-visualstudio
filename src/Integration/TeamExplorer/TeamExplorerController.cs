//-----------------------------------------------------------------------
// <copyright file="TeamExplorerController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.Shell;
using SonarLint.VisualStudio.Integration.Resources;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    [Export(typeof(ITeamExplorerController))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class TeamExplorerController : ITeamExplorerController
    {
        private readonly ITeamExplorer teamExplorer;

        internal /* testing purposes */ ITeamExplorer TeamExplorer => this.teamExplorer;

        [ImportingConstructor]
        public TeamExplorerController([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            this.teamExplorer = serviceProvider.GetService<ITeamExplorer>();
            if (this.TeamExplorer == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Strings.MissingService, nameof(ITeamExplorer)), nameof(serviceProvider));
            }
        }

        public void ShowSonarQubePage()
        {
            Debug.Assert(this.TeamExplorer != null, "Shouldn't be created without the Team Explorer service");
            this.TeamExplorer.NavigateToPage(new Guid(SonarQubePage.PageId), null);
        }
    }
}
