//-----------------------------------------------------------------------
// <copyright file="ConfigurableTeamExplorerController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.TeamExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SonarLint.VisualStudio.Integration.UnitTests
{
    public class ConfigurableTeamExplorerController : ITeamExplorerController
    {
        private int showConnectionsPageCalls = 0;

        void ITeamExplorerController.ShowConnectionsPage()
        {
            this.showConnectionsPageCalls++;
        }

        public void AssertExpectedNumCallsShowConnectionsPage(int calls)
        {
            Assert.AreEqual(calls, this.showConnectionsPageCalls, "Unexpected number of calls to ShowConnectionsPage");
        }
    }
}