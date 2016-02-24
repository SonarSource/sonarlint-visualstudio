//-----------------------------------------------------------------------
// <copyright file="SonarQubePage.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.TeamFoundation.Controls;
using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;
using SonarLint.VisualStudio.Integration.Resources;

namespace SonarLint.VisualStudio.Integration.TeamExplorer
{
    [TeamExplorerPage(SonarQubePage.PageId, MultiInstances = false)]
    internal class SonarQubePage : TeamExplorerPageBase
    {
        public const string PageId = "363C977C-DF9A-4298-9214-1247D9C846D8";

        public SonarQubePage()
        {
            this.Title = Strings.TeamExplorerPageTitle;
        }
    }
}
