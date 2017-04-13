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
